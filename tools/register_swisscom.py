#!/usr/bin/env python3

from selenium import webdriver
import multiprocessing
import time
import sys


password = 'P2019myc'


def retry(x):
    while True:
        try:
            if x() != False:
                break
        except:
            time.sleep(1)


def register(account):
    email = account[1]
    driver = webdriver.Chrome()
    z = email[0] * 2
    driver.get('https://www.swisscom.ch/en/residential/mycloud/registrieren.html')
    retry(lambda: driver.find_elements_by_css_selector('button[data-omni-action="Create new Login"]')[0].click())
    retry(lambda: driver.find_element_by_id('email').send_keys(email))
    driver.find_element_by_id('lastName').send_keys(z)
    driver.find_element_by_id('firstName').send_keys(z)
    driver.find_element_by_id('agbDesc').click()
    driver.find_elements_by_css_selector('.select__button')[0].click()
    retry(lambda: driver.find_elements_by_css_selector('.dropdown-item[data-value=MR]')[0].click())
    driver.find_element_by_id('submitButton').click()
    retry(lambda: driver.find_element_by_id('password').send_keys(password))
    driver.find_element_by_id('repeat-password').send_keys(password)
    driver.find_element_by_id('captcha-input-field').click()
    retry(lambda: driver.find_element_by_id('confirmation-btn').click())
    while True:
        boxes = driver.find_elements_by_css_selector('label[for]')
        if boxes:
            for b in boxes:
                b.click()
            break
        time.sleep(1)
    driver.find_element_by_tag_name('button').click()
    retry(lambda: driver.find_element_by_css_selector('div.mono-home2-usage-legend-amount').text == '0 B / 10 GB used')
    print(f'{account[0]} done.')
    driver.quit()


def get_mail(account):
    v = int(account[1:], 16)
    s = account[0]
    for i in range(16):
        if v >> i & 1:
            s += f'.{i:x}'
        else:
            s += f'{i:x}'
    return s + '@gmail.com'


if __name__ == "__main__":
    spec = sys.argv[1]
    accounts = [(f'{spec}{i:x}', get_mail(f'{spec}{i:x}')) for i in range(pow(16, 5 - len(spec)))]
    for account in accounts:
        print(f'    {account[0]}:')
        print(f'        Username: {account[1]}')
        print(f'        Password: {password}')
    print()
    for account in accounts:
        print(f'        - {account[0]}')
    print()

    multiprocessing.Pool(16).map(register, accounts)