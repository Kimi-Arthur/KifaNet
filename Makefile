SHELL := /bin/zsh

.PHONY: publish

bin_folder := /usr/local/bin
lib_folder := /usr/local/lib/Pimix

sdk_version := netcoreapp2.1

ifeq ($(shell uname), Darwin)
	os_version := osx-x64
else
	os_version := linux-x64
endif

all: build install

dev: build install_dev

staging: build install_staging

linux: os_version=linux-x64
linux: build

mac: os_version=osx-x64
mac: build

win10: os_version=win10-x64
win10: build

win: os_version=win-x64
win: build

setup:
	sudo ln -sf ${lib_folder}/fileutil/fileutil ${bin_folder}/fileutil
	sudo ln -sf ${lib_folder}/jobutil/jobutil ${bin_folder}/jobutil
	sudo ln -sf ${lib_folder}/subutil/subutil ${bin_folder}/subutil

publish:
	rm -rf publish
	dotnet pack -o ../../publish -c Release --include-symbols ${target}
	dotnet nuget push publish/*.symbols.nupkg -s https://api.nuget.org/v3/index.json -k oy2bkvsw65bqe4clccfawdv2s25hqmbe7ccuiph6yowrmq

build:
	dotnet publish -c Release src/Pimix.Apps.FileUtil
	dotnet publish -c Release src/Pimix.Apps.JobUtil
	dotnet publish -c Release src/Pimix.Apps.SubUtil

install:
	rm -rf ${lib_folder}
	mkdir -p ${lib_folder}/fileutil
	mkdir -p ${lib_folder}/jobutil
	mkdir -p ${lib_folder}/subutil
	cp -R src/Pimix.Apps.FileUtil/bin/Release/${sdk_version}/publish/* ${lib_folder}/fileutil
	cp -R src/Pimix.Apps.JobUtil/bin/Release/${sdk_version}/publish/* ${lib_folder}/jobutil
	cp -R src/Pimix.Apps.SubUtil/bin/Release/${sdk_version}/publish/* ${lib_folder}/subutil

install_dev:
	rm -rf ${lib_folder}
	mkdir -p ${lib_folder}/fileutil_dev
	mkdir -p ${lib_folder}/jobutil_dev
	mkdir -p ${lib_folder}/subutil_dev
	cp -R src/Pimix.Apps.FileUtil/bin/Release/${sdk_version}/publish/* ${lib_folder}/fileutil_dev
	cp -R src/Pimix.Apps.JobUtil/bin/Release/${sdk_version}/publish/* ${lib_folder}/jobutil_dev
	cp -R src/Pimix.Apps.SubUtil/bin/Release/${sdk_version}/publish/* ${lib_folder}/subutil_dev

install_staging:
	mkdir -p /usr/local/lib/pimix/fileutil_staging
	mkdir -p /usr/local/lib/pimix/jobutil_staging
	mkdir -p /usr/local/lib/pimix/subutil_staging
	cp -R src/Pimix.Apps.FileUtil/bin/Release/netcoreapp2.1/publish/* /usr/local/lib/pimix/fileutil_staging
	cp -R src/Pimix.Apps.JobUtil/bin/Release/netcoreapp2.1/publish/* /usr/local/lib/pimix/jobutil_staging
	cp -R src/Pimix.Apps.SubUtil/bin/Release/netcoreapp2.1/publish/* /usr/local/lib/pimix/subutil_staging

clean:
	dotnet clean

