pub extern fn ExitProcess(code: c_uint) noreturn;

fn fib(n: i32) i32 {
  var last: i32 = 1;
  var prelast: i32 = 0;
  var i: i32 = n;
  while (i >= 0) {
    const tmp = last + prelast;
    prelast = last;
    last = tmp;
    i -= 1;
  }

  return last;
}

pub fn main() void {
  var result: i32 = 0;
  var i: i32 = 0;

  while (i <= 40) {
    result += fib(i);
    i += 1;
  }
  
  ExitProcess(@bitCast(c_uint, result));
}
