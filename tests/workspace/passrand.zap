import io;
import string;

@[extern("c_int_rand"), header("c_rand.c")]
func rand(min: i32, max: i32): i32;

@[extern("c_void_init_rand"), header("c_rand.c")]
func init_rand();

func generate(char_count: u8): str {
	var buf = new [chr, char_count] { };
	var i: u8;

	while i < char_count {
		buf[i] = rand(33, 126) as chr;
		i++;
	}

	return buf as str;
}

func main() {
	init_rand();
	println("Random Password: " + generate(16 as u8));
}