func malloc(size: i64): unknown
func free(addr: unknown)

func heap<T>(value: T): *T {
	const allocation = malloc(size<T>()) as *T
	*allocation = value
	return allocation
}

func (self: *T) free<T>() {
	free(self as unknown)
}

func heap_arr<T>(): *T {

}