SHELL := /bin/zsh

sln_file = Pimix.mono40.sln

projects = fileutil jobutil
file_types = (exe.config|exe|dll)
binary_path = /usr/local/lib/pimix/
build_config = /p:TargetFrameworkVersion="v4.5" /p:Configuration=Release

all: build install

build:
	nuget restore ${sln_file}
	xbuild ${build_config} ${sln_file}

install:
	mkdir -p ${binary_path}
	$(foreach project,$(projects),cp $(project)/bin/Release/*.$(file_types) $(binary_path);)

clean:
	xbuild /t:Clean ${build_config} ${sln_file}

