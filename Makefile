SHELL := /bin/zsh

projects = fileutil
file_types = (exe.config|exe|dll)
binary_path = /usr/local/lib/pimix/

all: build install

build:
	xbuild /p:TargetFrameworkVersion="v4.5" /p:Configuration=Release Utilities.mono40.sln

install:
	mkdir -p ${binary_path}
	cp ${projects}/bin/Release/*.${file_types} ${binary_path}

