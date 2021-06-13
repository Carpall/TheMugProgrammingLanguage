from threading import Thread
from os import system as sh
from os import walk
from time import time

program_for_overhead = input("file name of an empty stripped exe: ")

programs = [f for _, _, fnames in walk(".") for f in fnames if f.endswith(".exe") and f != program_for_overhead]

start = time()
sh(program_for_overhead)
end = time()
overhead = end - start
times = int(input("times: "))
results = []

print("times:", times, "| programs:", programs, "| calculated overhead:", overhead)

def remove_overhead(elap):
    return elap - overhead * times

def bench_program(program):
    start = time()
    for i in range(times):
        sh(program)
    end = time()
    return remove_overhead(end - start)

def insert_in_results(program, time):
    for index, result in enumerate(results):
        if time < result[1]:
            results.insert(index, (program, time))
            return

    results.append((program, time))
    
for program in programs:
    insert_in_results(program, bench_program(program))

for index, program in enumerate(results):
    print(f"{index+1}. {program[0]}: {program[1]}")
