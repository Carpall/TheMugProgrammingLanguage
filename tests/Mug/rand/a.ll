; ModuleID = 'a.ll'
source_filename = "a.ll"

define void @println(i32 %0) {
entry:
  ret void
}

define void @main() {
entry:
  br i1 true, label %then, label %end

then:                                             ; preds = %entry
  call void @println(i32 1)
  br label %end

end:                                              ; preds = %then, %entry
  ret void
}
