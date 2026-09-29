"""Exercise the exported APK through real Android touch events; keep screenshots."""
from pathlib import Path
import struct
import subprocess
import time

package = 'ru.rubezh.frontier2.preview'
out = Path('android-check')
out.mkdir(exist_ok=True)

def adb(*args):
    return subprocess.check_output(['adb', *args])

def capture(name):
    data = adb('exec-out', 'screencap', '-p')
    assert data.startswith(b'\x89PNG'), 'Invalid device screenshot'
    (out / f'{name}.png').write_bytes(data)
    return struct.unpack('>II', data[16:24])

def launch():
    adb('shell', 'am', 'force-stop', package)
    adb('logcat', '-c')
    adb('shell', 'monkey', '-p', package, '-c', 'android.intent.category.LAUNCHER', '1')
    for _ in range(45):
        if b'RUBEZH_SCREEN=MenuView' in adb('logcat', '-d'):
            time.sleep(3)
            return
        time.sleep(1)
    raise RuntimeError('Game did not reach its C# main menu')

def tap(x, y):
    adb('shell', 'input', 'tap', str(round(x * width / 1280)), str(round(y * height / 800)))
    time.sleep(3)

def assert_screen(name):
    logs = adb('logcat', '-d').decode(errors='replace')
    (out / f'{name}.log').write_text(logs)
    assert f'RUBEZH_SCREEN={name}' in logs, f'{name} did not open from touch'
    assert adb('shell', 'pidof', package).strip(), 'Android game process exited'
    for error in ('FATAL EXCEPTION', 'FATAL UNHANDLED EXCEPTION', 'SCRIPT ERROR', 'Failed to load .NET runtime'):
        assert error not in logs, error

adb('install', '-r', 'native/Game/exports/android/Rubezh-0.1.0-preview.apk')
launch()
width, height = capture('menu')
assert width > height, 'Game must run in landscape'
tap(120, 626)
assert_screen('ArmoryView')
capture('armory')
launch()
tap(208, 445)
assert_screen('BattleView')
capture('battle-preparation')
tap(416, 205)
tap(416, 279)
tap(1140, 700)
time.sleep(8)
assert_screen('BattleView')
capture('battle')
print('ANDROID_SMOKE_OK: signed APK installed; C# menu, armory and battle opened by touch.')
