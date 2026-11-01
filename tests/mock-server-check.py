"""Run both built demos against private loopback responders; never connect to MATRIC."""
from pathlib import Path
import concurrent.futures
import json
import socket
import subprocess
import threading
import time

ROOT = Path(__file__).resolve().parents[1]


def check_demo(folder):
    """Validate actual executable targeting, readiness, event reception and completion."""
    executable = ROOT / 'demos' / folder / 'bin' / 'Release' / 'IntegrationDemo.exe'
    with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as server:
        server.bind(('127.0.0.1', 0))
        server.settimeout(0.1)
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as reservation:
            reservation.bind(('127.0.0.1', 0))
            receive_port = reservation.getsockname()[1]
        process = subprocess.Popen([
            str(executable), f'--port={server.getsockname()[1]}', f'--listen-port={receive_port}',
            '--client-id=second', '--timeout-ms=3000'
        ], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding='utf-8', errors='replace')
        output = []
        event_during_animation = threading.Event()
        def read_output():
            """Observe callbacks before animation completion instead of just checking buffered final output."""
            for line in process.stdout:
                output.append(line)
                if 'Control interaction:' in line and not any('Demo complete' in prior for prior in output):
                    event_during_animation.set()
        reader = threading.Thread(target=read_output, daemon=True)
        reader.start()
        requests = 0
        deck = None
        deck_polls = 0
        ready = False
        switches = 0
        state_commands = 0
        variable_batches = 0
        deadline = time.monotonic() + 60
        def reply(kind, data, endpoint):
            """Use the unchanged envelope with a nested JSON string and trailing newline."""
            wire = {'MessageType': kind, 'MessageData': json.dumps(data, ensure_ascii=False)}
            server.sendto((json.dumps(wire, ensure_ascii=False) + '\n').encode(), endpoint)
        try:
            while process.poll() is None:
                if time.monotonic() > deadline:
                    raise AssertionError(f'{folder}: executable did not finish')
                try:
                    payload, endpoint = server.recvfrom(65535)
                except socket.timeout:
                    continue
                message = json.loads(payload.decode('utf-8'))
                command = message['command']
                if command == 'GETCONNECTEDCLIENTS':
                    requests += 1
                    if requests == 1:
                        reply('ClientList', [], endpoint)
                        continue
                    if deck:
                        deck_polls += 1
                        ready = deck_polls >= 3
                    reply('ClientList', [
                        {'clientId': 'first', 'clientName': 'Other device', 'currentDeckId': 'other'},
                        {'clientId': 'second', 'clientName': 'Željko tablet', 'currentDeckId': deck if ready else None}
                    ], endpoint)
                    continue
                if command == 'SETVARIABLES':
                    variable_batches += 1
                    variables = message['Variables']
                    assert len({v['Name'] for v in variables}) == len(variables), 'Duplicate variable names'
                    structured = next(v for v in variables if v['Name'] == 'demo_json')
                    assert isinstance(structured['Value'], dict) and isinstance(structured['Value']['propNumber'], int)
                    reply('VariablesChange', {'ChangedVariables': [v['Name'] for v in variables],
                                              'Variables': {v['Name']: v for v in variables}}, endpoint)
                    continue
                assert message.get('clientId') == 'second', f'Unexpected broadcast or wrong device: {message}'
                if command == 'SETDECK':
                    switches += 1
                    deck = message['deckId']
                else:
                    assert ready, f'Visual commands started before reported deck membership: {command}'
                    state_commands += 1
                if command == 'SETCONTROLSSTATE':
                    for control in message['data']:
                        assert isinstance(control['state'], dict)
                        if 'position' in control['state']:
                            assert 0 <= control['state']['position'] <= 4
                reply('ControlInteraction', {'MessageType': 'controlevent',
                      'ClientInfo': {'clientId': 'second'},
                      'MessageData': {'DeckId': deck, 'ControlName': 'Željko 😉', 'EventName': 'press', 'Data': {'pressed': True}}}, endpoint)
            reader.join(timeout=2)
            errors = process.stderr.read()
            assert process.returncode == 0, f'{folder}: {errors}\n'+''.join(output)
            assert switches == 1, f'{folder}: animation restarted or deck switch was retried'
            assert state_commands > 100 and event_during_animation.is_set(), f'{folder}: events were blocked'
            assert any('Demo complete' in line for line in output)
            if folder == 'net_wrapper':
                assert variable_batches == 2
                assert any('Server variables changed' in line for line in output)
            assert not errors, errors
            print(f'PASS {folder}: one animation, selected device only, deck readiness, live events, clean exit.', flush=True)
        finally:
            if process.poll() is None:
                process.kill()
                process.wait(timeout=5)
            reader.join(timeout=2)
            process.stdout.close()
            process.stderr.close()


if __name__ == '__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        list(pool.map(check_demo, ['raw', 'net_wrapper']))
