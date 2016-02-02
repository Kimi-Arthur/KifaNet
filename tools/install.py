#!/usr/bin/env python3
import codecs
import json
import os
import platform
import pystache
import shutil
import subprocess
import sys
from colorama import init, Fore

init(autoreset=True)

is_windows = platform.system() == 'Windows'

def download_packages(data):
    for p in data['packages']:
        prerelease = p['prerelease'] if 'prerelease' in p else data['prerelease']
        name = p['name']
        version = p['version']
        print(Fore.GREEN + 'Downloading {}package named "{}" at {}...'.format('prerelease ' if prerelease else '', name, version))
        subprocess.run(
                [
                    'nuget',
                    'install', name,
                    '-Version', version,
                    '-OutputDirectory', data['lib_prefix'],
                    '-PackageSaveMode', 'nuspec'
                ] +
                (['-Prerelease'] if prerelease else []))


def copy_binaries(data):
    bin_path = '{}/{}'.format(data['bin_prefix'], data['name'])
    os.makedirs(bin_path, exist_ok=True)

    for p in data['packages']:
        for entry in os.scandir("{lib_prefix}/{name}.{version}/lib/{framework}/".format(lib_prefix=data['lib_prefix'], **p)):
            shutil.copy(entry.path, "{bin_path}/{entry_name}".format(bin_path=bin_path, entry_name=entry.name, **p))


def write_runner(data):
    tmpl = codecs.open("runner.sh.mustache", "r", "utf-8").read()
    path = '{}/{}'.format(data['bin_prefix'], data['name'])
    print(Fore.GREEN + 'Writing runner script to {}...'.format(path))
    codecs.open(path, 'w', 'utf-8').write(pystache.render(tmpl, data))
    os.chmod(path, 0o755)


def prepare_data(data):
    if 'install_prefix' not in data:
        data['install_prefix'] = 'C:/Software' if is_windows else '/usr/local'

    data['lib_prefix'] = data['install_prefix'] + '/lib/pimix'
    data['bin_prefix'] = data['install_prefix'] + '/bin'

    os.makedirs(data['lib_prefix'], exist_ok=True)
    os.makedirs(data['bin_prefix'], exist_ok=True)

    for p in data['packages']:
        if 'binary' in p:
            data['binary'] = p
            break
    for k, v in data['defaults'].items():
        for p in data['packages']:
            if k not in p:
                p[k] = v
    return data


def install(name):
    data = prepare_data(json.load(codecs.open(name + '.json', 'r', 'utf-8')))
    download_packages(data)
    if is_windows:
        copy_binaries(data)
    else:
        write_runner(data)


def main():
    for pkg in sys.argv[1:]:
        install(pkg)


if __name__ == '__main__':
    main()
