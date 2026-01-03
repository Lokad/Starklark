# Subset of test_suite/testdata/go misc coverage.

assert_eq(type(1), "int")
assert_eq(type("a"), "string")
assert_eq(type([1]), "list")
assert_eq(type((1,)), "tuple")
---
nan = float("nan")
assert_(nan != nan)
assert_(not (nan < 1))
assert_(not (nan > 1))
assert_(not (nan <= 1))
assert_(not (nan >= 1))
---
assert_(hasattr("abc", "upper"))
assert_(not hasattr("abc", "nope"))
assert_eq(getattr("abc", "upper")(), "ABC")
---
cyclic_list = []
cyclic_list.append(cyclic_list)
repr(cyclic_list) ### (maximum recursion)
cyclic_list == cyclic_list ### (maximum recursion)
---
cyclic_dict = {}
cyclic_dict["self"] = cyclic_dict
repr(cyclic_dict) ### (maximum recursion)
cyclic_dict == cyclic_dict ### (maximum recursion)
