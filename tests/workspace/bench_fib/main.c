#include <stdio.h>

int fib(int n) {
  if (n < 2) { return n; } else { return fib(n - 1) + fib(n - 2); }
}

int main() {
  int i = 0;
	const int cycles = 50;

	for (; i < cycles; i++) {
		printf("%d", fib(i));
	}
}