# Subset of go suite to cover string literal forms and empty tuples.

assert_eq('a', "a")
assert_eq(r"\n", "\\n")
assert_eq((), tuple())
---
assert_eq(1 if True else 0, 1)
