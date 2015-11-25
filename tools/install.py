#!/usr/bin/env python3
import codecs
import json
import os
import pystache
import subprocess
import sys
from colorama import init, Fore

init(autoreset=True)


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


def write_runner(data):
    tmpl = codecs.open("runner.sh.mustache", "r", "utf-8").read()
    path = '{}/{}'.format(data['bin_prefix'], data['name'])
    print(Fore.GREEN + 'Writing runner script to {}...'.format(path))
    codecs.open(path, 'w', 'utf-8').write(pystache.render(tmpl, data))
    os.chmod(path, 0o755)


def prepare_data(data):
    if 'install_prefix' not in data:
        data['install_prefix'] = '/usr/local'

    data['lib_prefix'] = data['install_prefix'] + '/lib/pimix'
    data['bin_prefix'] = data['install_prefix'] + '/bin'

    for p in data['packages']:
        if 'binary' in p:
            data['binary'] = p
            break
    for k, v in data['defaults'].items():
        data[k] = v
    return data


def install(name):
    data = prepare_data(json.load(codecs.open(name + '.json', 'r', 'utf-8')))
    download_packages(data)
    write_runner(data)


def main():
    for pkg in sys.argv[1:]:
        install(pkg)


if __name__ == '__main__':
    main()
