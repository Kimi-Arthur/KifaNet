#!/usr/bin/env python3


def download_packages(data):
    pass


def write_runner(data):
    pass


def prepare_data(data):
    return data


def install(name):
    data = prepare_data(json.load(name + '.json', 'r', 'utf-8'))
    download_packages(data)
    write_runner(data)


def main():
    pass


if __name__ == '__main__':
    main()
