import io;
import string;
import "stack.zap";

func strlen(text: str): i64;
func malloc(size: i64): unknown;

func get_acronym(text: str): Stack<chr> {
	var i: i32;
	var taken = false;
	var result = Stack<chr>(10);
	const len = strlen(text) as i32;

	while i < len {
		const cur = text[i];
		if cur == ' ' {
			taken = false;
		} elif !taken {
			(&result).push<chr>(cur);
			taken = true;
		}
		
		i++;
	}

	return result;
}

func (self: chr) to_str(): str {
  const allocation = malloc(2 as i64) as str;
  allocation[0] = self;
  allocation[1] = '\0';

  return allocation;
}

func get_initial_chars(chars: Stack<chr>): str {
	const len = chars.len;
	var result = new [chr, len + 1] { };
	var i: i32;

	while i < len {
		result[len - i - 1] = (&chars).pop<chr>();
		i++;
	}

	result[len] = '\0';

	return result as str;
}

func main() {
	const acronym = get_initial_chars(get_acronym(readln("meaning: ")));
	println("acronym: " + acronym);
}