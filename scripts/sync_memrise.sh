#!/bin/bash

datax sync ~/Projects/German/Data/goethe_word_list.yaml
memx levels ~/Projects/German/Data/goethe_word_list.yaml ~/Projects/German/Data/goethe_lists.yaml
datax sync ~/Projects/German/Data/goethe_lists.yaml
