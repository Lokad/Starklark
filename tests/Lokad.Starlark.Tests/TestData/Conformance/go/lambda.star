# Subset of test_suite/testdata/go lambda coverage.

f = lambda x: x + 1
assert_eq(f(2), 3)
---
f = lambda: 1
assert_eq(f(), 1)
---
f = lambda x, y=2: x + y
assert_eq(f(3), 5)
assert_eq(f(3, 4), 7)
---
f = lambda *args, **kwargs: [args, kwargs]
assert_eq(f(1, 2, a=3), [(1, 2), {"a": 3}])
---
x = 5
f = lambda y: x + y
assert_eq(f(2), 7)
---
assert_eq((lambda x: x)(4), 4)
---
lambda x=1, y: y ### (Non-default parameter 'y' follows default parameter in 'lambda'.)
