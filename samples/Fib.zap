import io;

func fib(n: i32): i32 {
	return
		if n < 2 ? n
		else (fib(n - 1) + fib(n - 2));
}

func main() {
	println(fib(15));
}