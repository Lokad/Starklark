# Subset of test_suite/testdata/java string_format coverage.

assert_eq("abc".format(), "abc")
assert_eq("x{key}x".format(key=2), "x2x")
assert_eq("{{}}".format(), "{}")
assert_eq("{test} and {}".format(2, test=1), "1 and 2")
assert_eq("{} {} {}".format(1, 2, 3), "1 2 3")
assert_eq("{0} {1}".format("a", "b"), "a b")
assert_eq("{0} {0}".format("a"), "a a")
assert_eq("{{{0}}}".format(42), "{42}")
---
"{}".format() ### (Not enough arguments for format)
---
"{} {1}".format(1, 2) ### (Cannot mix automatic and manual field numbering)
---
"{missing}".format(x=1) ### (Missing argument 'missing')
---
"{".format(1) ### (Unmatched '{')
---
"}".format(1) ### (Single '}' in format string)
---
"{0,1}".format(1, 2) ### (Invalid format field)
---
"{0:02d}".format(1) ### (Format specifiers are not supported)
