SHELL := /bin/zsh

sln_file = Utilities.mono40.sln

projects = fileutil
file_types = (exe.config|exe|dll)
binary_path = /usr/local/lib/pimix/

all: build install

build:
	nuget restore ${sln_file}
	xbuild /p:TargetFrameworkVersion="v4.5" /p:Configuration=Release ${sln_file}

install:
	mkdir -p ${binary_path}
	cp ${projects}/bin/Release/*.${file_types} ${binary_path}

