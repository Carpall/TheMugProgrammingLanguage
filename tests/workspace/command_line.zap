import io;
import string;
# import "stack.zap";
import memory;

func strlen(text: str): i64;

type CommandLine { command: str, args: [str], args_count: i32, echo_on: u1 }

import io;

pub type Stack<T> { raw: [T], len: i32, cap: i32 }

pub func Stack<T>(): Stack<T> { return Stack<T>(1000); }

pub func Stack<T>(capacity: i32): Stack<T> {
  return new Stack<T> {
    raw: new [T, capacity] { },
    len: 0,
    cap: capacity
  };
}

pub func (self: &Stack<T>) realloc<T>(capacity: i32) {
  self.cap = capacity;
  self.raw = realloc(self.raw as unknown, capacity as i64) as [T];
}

pub func (self: &Stack<T>) new_capacity<T>(): i32 {
  const MAX_CAP = 1000000000; # 1 miliardo
  
  if (self.cap*2 > MAX_CAP) { panic("reached max stack length"); }

  return self.cap*2;
}

pub func (self: &Stack<T>) push<T>(element: T) {

  if self.len >= self.cap { self.realloc<T>(self.new_capacity<T>()); }

  self.raw[self.len] = element;
  self.len++;
}

pub func (self: &Stack<T>) pop<T>(): T {
  if self.len == 0 { panic("called pop over empty stack"); }

  self.len--;
  return self.raw[self.len];
}

pub func realloc(allocation: unknown, size: i64): unknown;
pub func exit(code: i32);

pub func panic(err: str) {
  println(err);
  exit(1);
}

func trap(err: str, exit: u1) {
	println("Error: " + err);
	if exit { exit(1); }
}

func (self: &CommandLine) echo() {
	if !self.echo_on { return; }
	var i: i32;
	
	while i < self.args_count {
		print(self.args[i]);
		print(' ');
		i++;
	}
	
	print('\n');
}

@[header("ls.cpp"), clang_args("-std=c++17")]
func c_ls(dir: str);

func (self: &CommandLine) ls() {
	var dir = ".";

	if self.args_count > 0 { dir = self.args[0]; }
	
	c_ls(dir);
}

@[code("
#include <stdlib.h>

void $void_clear_screen_noargs() {
	#ifdef _WIN32
	system(\"cls\");
	#elif __linux__
	system(\"clear\");
	#endif
}
"), extern("$void_clear_screen_noargs")]
func clear();

@[header("readfile.cpp"), extern("$char_readfile_const_char")]
func c_read_file(path: str): str;

@[code("
int topointer(const char* s) {
	return (int)((void*)s);
}
")]
func topointer(string: str): i32;

error ReadFileError { BadPath }

func read_file(path: str): ReadFileError!str {
	const result = c_read_file(path);

	if topointer(result) == 0 { return ReadFileError.BadPath; }

	return result;
}

func (self: &CommandLine) print() {
	var i: i32;

	while i < self.args_count {
		if i > 0 { println("\n\n"); }

		const filename = self.args[i];
		const text = read_file(filename) catch { println("Not a file"); return; };
		
		println(text);
		i++;
	}
}

func (self: &CommandLine) interpret_command() {
	const command = self.command;

	if command == "cls" {
		clear();
	} elif command == "echo_off" {
		self.echo_on = false;
	} elif command == "echo_on" {
		self.echo_on = true;
	} elif command == "echo" {
		self.echo();
	} elif command == "ls" {
		self.ls();
	} elif command == "exit" {
		exit(0);
	} elif command == "print" {
		self.print();
	} else {
		trap("Invalid command `" + command + "`", false);
	}
}

func (self: str) split(char: chr): Stack<str> {
	const len = strlen(self) as i32;

	var i: i32;
	var result = Stack<str>(10);
	var builder: str;
	var builder_len: i32;

	while i < len {
		const cur = self[i];

		if cur == char {
			if builder_len > 0 {
				(&result).push<str>(builder);
				builder = "";
				builder_len = 0;
			}
		} else {
			builder += cur;
			builder_len++;
		}

		i++;
	}

	if builder_len > 0 { (&result).push<str>(builder); }

	return result;
}

func get_command_and_args(
	splitted: Stack<str>,
	command: &str,
	args: &[str],
	args_count: &i32) {

		args_count = splitted.len - 1;
		args = new [str, args_count] { };
		var i: i32;

		while i < args_count {
			args[args_count - i - 1] = (&splitted).pop<str>();
			i++;
		}

		command = (&splitted).pop<str>();
}

func main() {
	var cmd = new CommandLine { echo_on: true };

	while true {
		var prompt: str;
		if cmd.echo_on { prompt = "$> "; }

		var splitted = readln(prompt).split(' ');

		if splitted.len == 0 { continue; }
		
		get_command_and_args(splitted, &(cmd.command), &(cmd.args), &(cmd.args_count));
	
		(&cmd).interpret_command();
	}
}