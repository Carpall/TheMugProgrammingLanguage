import io;
import string;
import memory;
import "stack.zap";

func strlen(text: str): i64;

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

func main() {
	var splitted = readln("to reverse: ").split(' ');
	var i: i32;
	const len = splitted.len;

	while i < len {
		print((&splitted).pop<str>());
		print(' ');
		i++;
	}
}