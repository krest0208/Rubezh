"""Configure an isolated CI runner for Godot's prebuilt .NET Android templates."""
import json
import os
from pathlib import Path
import zipfile

with zipfile.ZipFile('/tmp/godot-templates.tpz') as archive:
    version_file = next(n for n in archive.namelist() if n.endswith('/version.txt'))
    version = archive.read(version_file).decode().strip()
    target = Path.home() / '.local/share/godot/export_templates' / version
    target.mkdir(parents=True, exist_ok=True)
    for filename in ('android_debug.apk', 'android_release.apk'):
        name = next(n for n in archive.namelist() if n.endswith('/' + filename))
        (target / filename).write_bytes(archive.read(name))
    print('Android templates installed:', version)

settings = Path.home() / '.config/godot/editor_settings-4.5.tres'
settings.parent.mkdir(parents=True, exist_ok=True)
sdk = os.environ['ANDROID_HOME']
java = os.environ['JAVA_HOME']
settings.write_text(
    '[gd_resource type="EditorSettings" format=3]\n\n[resource]\n'
    f'export/android/android_sdk_path = {json.dumps(sdk)}\n'
    f'export/android/java_sdk_path = {json.dumps(java)}\n'
)
