SHELL := /bin/zsh

all: build install

build:
	dotnet restore
	dotnet build -c Release

install:
	mkdir -p /usr/local/lib/pimix/fileutil
	mkdir -p /usr/local/lib/pimix/jobutil
	cp src/Pimix.Apps.FileUtil/bin/Release/netcoreapp2.0/* /usr/local/lib/pimix/fileutil
	cp src/Pimix.Apps.JobUtil/bin/Release/netcoreapp2.0/* /usr/local/lib/pimix/jobutil

clean:
	dotnet clean

