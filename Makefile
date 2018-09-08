SHELL := /bin/zsh

all: build install

dev: build install_dev

staging: build install_staging

build:
	dotnet publish -c Release src/Pimix.Apps.FileUtil
	dotnet publish -c Release src/Pimix.Apps.JobUtil
	dotnet publish -c Release src/Pimix.Apps.SubUtil

install:
	mkdir -p /usr/local/lib/pimix/fileutil
	mkdir -p /usr/local/lib/pimix/jobutil
	mkdir -p /usr/local/lib/pimix/subutil
	cp -R src/Pimix.Apps.FileUtil/bin/Release/netcoreapp2.0/publish/* /usr/local/lib/pimix/fileutil
	cp -R src/Pimix.Apps.JobUtil/bin/Release/netcoreapp2.0/publish/* /usr/local/lib/pimix/jobutil
	cp -R src/Pimix.Apps.SubUtil/bin/Release/netcoreapp2.1/publish/* /usr/local/lib/pimix/subutil

install_dev:
	mkdir -p /usr/local/lib/pimix/fileutil_dev
	mkdir -p /usr/local/lib/pimix/jobutil_dev
	mkdir -p /usr/local/lib/pimix/subutil_dev
	cp -R src/Pimix.Apps.FileUtil/bin/Release/netcoreapp2.0/publish/* /usr/local/lib/pimix/fileutil_dev
	cp -R src/Pimix.Apps.JobUtil/bin/Release/netcoreapp2.0/publish/* /usr/local/lib/pimix/jobutil_dev
	cp -R src/Pimix.Apps.SubUtil/bin/Release/netcoreapp2.1/publish/* /usr/local/lib/pimix/subutil_dev

install_staging:
	mkdir -p /usr/local/lib/pimix/fileutil_staging
	mkdir -p /usr/local/lib/pimix/jobutil_staging
	mkdir -p /usr/local/lib/pimix/subutil_staging
	cp -R src/Pimix.Apps.FileUtil/bin/Release/netcoreapp2.0/publish/* /usr/local/lib/pimix/fileutil_staging
	cp -R src/Pimix.Apps.JobUtil/bin/Release/netcoreapp2.0/publish/* /usr/local/lib/pimix/jobutil_staging
	cp -R src/Pimix.Apps.SubUtil/bin/Release/netcoreapp2.1/publish/* /usr/local/lib/pimix/subutil_staging

clean:
	rm -rf /usr/local/lib/pimix
	dotnet clean

