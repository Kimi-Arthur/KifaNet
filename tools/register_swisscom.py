#!/usr/bin/env python3

from selenium import webdriver
import time
import sys


def retry(x):
    while True:
        try:
            x()
            break
        except:
            time.sleep(1)


def register(email):
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
    retry(lambda: driver.find_element_by_id('password').send_keys('P2019myc'))
    driver.find_element_by_id('repeat-password').send_keys('P2019myc')
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
    input(f'{email} done.')
    driver.quit()


if __name__ == "__main__":
    for arg in sys.argv[1:]:
        register(arg)
