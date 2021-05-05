func a(): str {
	var x = new [chr] { 'a', 'b', 'c' }

	/* for i = 97, i < 97 + 10, i++ {
		x[i - 97] = i as chr
	} */

	return x as str
}


/* 
  Generated Code
	
	define { i64, i8* }* @b() {
  	%1 = alloca { i64, i8* }*, align 8
  	%malloccall = tail call i8* bitcast (i8* (i64)* @malloc to i8* (i32)*)(i32 ptrtoint ({ i64, i8* }* getelementptr ({ i64, i8* }, { i64, i8* }* null, i32 1) to i32))
  	%2 = bitcast i8* %malloccall to { i64, i8* }*
  	store { i64, i8* }* %2, { i64, i8* }** %1, align 8
  	%3 = load { i64, i8* }*, { i64, i8* }** %1, align 8
  	%4 = getelementptr inbounds { i64, i8* }, { i64, i8* }* %3, i32 0, i32 0
  	store i64 1, i64* %4, align 8
  	%5 = alloca i8, i64 1, align 1
  	%6 = load { i64, i8* }*, { i64, i8* }** %1, align 8
  	%7 = getelementptr inbounds { i64, i8* }, { i64, i8* }* %6, i32 0, i32 1
  	store i8* %5, i8** %7, align 8
  	%8 = load { i64, i8* }*, { i64, i8* }** %1, align 8
  	%9 = getelementptr inbounds { i64, i8* }, { i64, i8* }* %8, i32 0, i32 1
  	%10 = load i8*, i8** %9, align 8
  	%11 = getelementptr i8, i8* %10, i32 0
  	store i8 97, i8* %11, align 1
  	%x = alloca { i64, i8* }*, align 8
  	store { i64, i8* }* %8, { i64, i8* }** %x, align 8
  	%12 = load { i64, i8* }*, { i64, i8* }** %x, align 8
  	ret { i64, i8* }* %12
	}
*/

func b(): [chr] {
	return new [chr] { 'a', 'b', 'c' }
}

func main() {
  var x = cstr!(b() as str)
}