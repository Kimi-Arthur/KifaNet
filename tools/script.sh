#!/bin/bash

if [ $1 -eq --debug ]
then
    mono --debug /usr/local/lib/pimix/fileutil/fileutil.exe "${@:2}"
else
    mono /usr/local/lib/pimix/fileutil/fileutil.exe "$@"
fi
