int fib(int n) {
  int last = 1;
  int prelast = 0;
  while (n >= 0) {
    const int tmp = last + prelast;
    prelast = last;
    last = tmp;
    n--;
  }

  return last;
}

int main() {
  int result = 0;

  for (int i = 0; i <= 40; i++)
    result += fib(i);

  return result;
}
