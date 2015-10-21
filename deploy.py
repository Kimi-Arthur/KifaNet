import os
import re
import shutil

destination = 'C:/Software/Pimix/'

apps = [
    'fileutil',
    'jobutil'
]

include_patterns = [
    r'\.exe',
    r'\.exe\.config',
    r'\.dll',
    r'\.pdb'
]

exclude_patterns = [
    'FSharp',
    'vshost'
]


os.makedirs(destination, exist_ok=True)

for app in apps:
    for entry in os.scandir('src/{}/bin/Release/'.format(app)):
        to_copy = False
        for p in include_patterns:
            if re.search(p, entry.path):
                to_copy = True
                break
        if not to_copy:
            continue
        for p in exclude_patterns:
            if re.search(p, entry.path):
                to_copy = False
                break
        if to_copy:
            shutil.copyfile(entry.path, '{}{}'.format(destination, entry.name))
