import std;

use std.io as io;

type Err[type ResultType]
{
	err: bit,
	kind: u8,
	message: str,
	result: ResultType
}
func Bad[type ResultType](kind: u8, error_msg: str): Err[ResultType]
{
	return new Err[ResultType] {
		err: true,
		kind: kind,
		message: error_msg
	};
}
func Good[type ResultType](result: ResultType): Err[ResultType]
{
	return new Err[ResultType] {
		err: false,
		result: result
	};
}

func discard(self: Err[type ResultType]): ResultType
{
	if self.err
	{
		io.println("Panic: "+self.message+". Exit code: "+self.kind);
		sys_crash(self.kind);
	}
	return self.result;
}
func is_err(self: Err[type ResultType]): bit
{
	return self.err;
}
func error(self: Err[type ResultType]): str
{
	return self.message;
}
func kind(self: Err[type ResultType]): u8
{
	return self.kind;
}



type Err
{
	err: bit,
	kind: u8,
	message: str
}
func Bad(kind: u8, error_msg: str): Err
{
	return new Err {
		err: true,
		kind: kind,
		message: error_msg
	};
}
func Good(): Err
{
	return new Err {
		err: false
	};
}

func is_err(self: Err): bit
{
	return self.err;
}
func error(self: Err): str
{
	return self.message;
}
func kind(self: Err): u8
{
	return self.kind;
}