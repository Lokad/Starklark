# Subset of go suite to cover string literal forms and empty tuples.

assert_eq('a', "a")
assert_eq(r"\n", "\\n")
assert_eq((), tuple())
assert_eq("multi\nline", """multi
line""")
assert_eq("multi\nline", '''multi
line''')
assert_eq(bytes([65, 66, 67]), b"ABC")
assert_eq(bytes([92, 110]), br"\n")
---
assert_eq(1 if True else 0, 1)
