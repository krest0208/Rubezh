"""Reject incomplete exports even if Godot's exporter returns exit code zero."""
import sys
import zipfile

with zipfile.ZipFile(sys.argv[1]) as apk:
    assert apk.testzip() is None, 'Corrupted APK entry'
    names = apk.namelist()
    for abi in ('arm64-v8a', 'x86_64'):
        assert any(n.startswith(f'lib/{abi}/libmono') for n in names), f'Missing .NET runtime for {abi}'
    for language in ('ru', 'en', 'de', 'fr', 'zh', 'ja'):
        assert f'assets/Data/campaign.{language}.json.gz' in names, language
    assert any('Rubezh.dll' in n for n in names), 'Missing C# game assembly'
    assert any('Rubezh.Core.dll' in n for n in names), 'Missing C# simulation assembly'
    print('APK_CONTENT_OK: both 64-bit .NET runtimes, game assemblies and six campaigns.')
