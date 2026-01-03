# Subset of go suite patterns for assert helper behavior.

assert_(0) ### (assert_ failed)
---
assert_eq(1, 2) ### (assert_eq failed)
---
assert_ne(1, 1) ### (assert_ne failed)
---
assert_eq(1) ### (expects 2 arguments)
---
assert_(1, 2) ### (expects 1 arguments)
