SHELL := /bin/zsh

all: build install

build:
	dotnet restore
	dotnet publish -c Release

install:
	mkdir -p /usr/local/lib/pimix/fileutil
	mkdir -p /usr/local/lib/pimix/jobutil
	cp -R src/Pimix.Apps.FileUtil/bin/Release/netcoreapp2.0/publish/* /usr/local/lib/pimix/fileutil
	cp -R src/Pimix.Apps.JobUtil/bin/Release/netcoreapp2.0/publish/* /usr/local/lib/pimix/jobutil

clean:
	rm -rf /usr/local/lib/pimix
	dotnet clean

