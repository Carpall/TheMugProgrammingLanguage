	.text
	.def	 @feat.00;
	.scl	3;
	.type	0;
	.endef
	.globl	@feat.00
.set @feat.00, 0
	.file	"main.c"
	.def	 fib;
	.scl	2;
	.type	32;
	.endef
	.globl	fib                             # -- Begin function fib
	.p2align	4, 0x90
fib:                                    # @fib
.seh_proc fib
# %bb.0:
	pushq	%rsi
	.seh_pushreg %rsi
	pushq	%rdi
	.seh_pushreg %rdi
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	movl	%ecx, %edi
	xorl	%esi, %esi
	cmpl	$2, %ecx
	jge	.LBB0_2
# %bb.1:
	movl	%edi, %ecx
	jmp	.LBB0_4
.LBB0_2:
	xorl	%esi, %esi
	.p2align	4, 0x90
.LBB0_3:                                # =>This Inner Loop Header: Depth=1
	leal	-1(%rdi), %ecx
	callq	fib
	leal	-2(%rdi), %ecx
	addl	%eax, %esi
	cmpl	$3, %edi
	movl	%ecx, %edi
	jg	.LBB0_3
.LBB0_4:
	addl	%ecx, %esi
	movl	%esi, %eax
	addq	$40, %rsp
	popq	%rdi
	popq	%rsi
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 main;
	.scl	2;
	.type	32;
	.endef
	.globl	main                            # -- Begin function main
	.p2align	4, 0x90
main:                                   # @main
.seh_proc main
# %bb.0:
	pushq	%rsi
	.seh_pushreg %rsi
	subq	$32, %rsp
	.seh_stackalloc 32
	.seh_endprologue
	xorl	%ecx, %ecx
	callq	fib
	leaq	"??_C@_02DPKJAMEF@?$CFd?$AA@"(%rip), %rsi
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$1, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$2, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$3, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$4, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$5, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$6, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$7, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$8, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$9, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$10, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$11, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$12, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$13, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$14, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$15, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$16, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$17, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$18, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$19, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$20, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$21, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$22, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$23, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$24, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$25, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$26, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$27, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$28, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$29, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$30, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$31, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$32, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$33, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$34, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$35, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$36, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$37, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$38, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$39, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$40, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$41, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$42, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$43, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$44, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$45, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$46, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$47, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$48, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	movl	$49, %ecx
	callq	fib
	movq	%rsi, %rcx
	movl	%eax, %edx
	callq	printf
	xorl	%eax, %eax
	addq	$32, %rsp
	popq	%rsi
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 printf;
	.scl	2;
	.type	32;
	.endef
	.section	.text,"xr",discard,printf
	.globl	printf                          # -- Begin function printf
	.p2align	4, 0x90
printf:                                 # @printf
.seh_proc printf
# %bb.0:
	pushq	%rsi
	.seh_pushreg %rsi
	pushq	%rdi
	.seh_pushreg %rdi
	pushq	%rbx
	.seh_pushreg %rbx
	subq	$48, %rsp
	.seh_stackalloc 48
	.seh_endprologue
	movq	%rcx, %rsi
	movq	%rdx, 88(%rsp)
	movq	%r8, 96(%rsp)
	movq	%r9, 104(%rsp)
	leaq	88(%rsp), %rbx
	movq	%rbx, 40(%rsp)
	movl	$1, %ecx
	callq	__acrt_iob_func
	movq	%rax, %rdi
	callq	__local_stdio_printf_options
	movq	(%rax), %rcx
	movq	%rbx, 32(%rsp)
	movq	%rdi, %rdx
	movq	%rsi, %r8
	xorl	%r9d, %r9d
	callq	__stdio_common_vfprintf
	nop
	addq	$48, %rsp
	popq	%rbx
	popq	%rdi
	popq	%rsi
	retq
	.seh_handlerdata
	.section	.text,"xr",discard,printf
	.seh_endproc
                                        # -- End function
	.def	 __local_stdio_printf_options;
	.scl	2;
	.type	32;
	.endef
	.section	.text,"xr",discard,__local_stdio_printf_options
	.globl	__local_stdio_printf_options    # -- Begin function __local_stdio_printf_options
	.p2align	4, 0x90
__local_stdio_printf_options:           # @__local_stdio_printf_options
# %bb.0:
	leaq	__local_stdio_printf_options._OptionsStorage(%rip), %rax
	retq
                                        # -- End function
	.section	.rdata,"dr",discard,"??_C@_02DPKJAMEF@?$CFd?$AA@"
	.globl	"??_C@_02DPKJAMEF@?$CFd?$AA@"   # @"??_C@_02DPKJAMEF@?$CFd?$AA@"
"??_C@_02DPKJAMEF@?$CFd?$AA@":
	.asciz	"%d"

	.lcomm	__local_stdio_printf_options._OptionsStorage,8,8 # @__local_stdio_printf_options._OptionsStorage
	.addrsig
	.addrsig_sym __local_stdio_printf_options._OptionsStorage
