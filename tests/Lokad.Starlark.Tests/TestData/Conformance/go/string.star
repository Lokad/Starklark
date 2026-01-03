# Subset of test_suite/testdata/go string coverage.

assert_eq("a\\bc", "a\\bc")
assert_("abc")
assert_(not "")
assert_eq("a" + "b", "ab")
assert_eq("ab" * 3, "ababab")
---
assert_eq("abc"[0], "a")
"abc"[3] ### (Index out of range)
---
assert_eq("banana".replace("a", "o", 1), "bonana")
assert_eq("foofoo".find("oo"), 1)
assert_eq("foofoo".rfind("oo"), 4)
---
assert_("foo".startswith("fo"))
assert_("foo".endswith("oo"))
---
assert_eq(",".join(["a", "b", "c"]), "a,b,c")
assert_eq("a.b.c".split("."), ["a", "b", "c"])
---
assert_eq("hello, world!".capitalize(), "Hello, world!")
assert_("hello, world".islower())
assert_(not "Catch-22".islower())
assert_("HAL-9000".isupper())
assert_(not "Catch-22".isupper())
assert_("Hello, World!".istitle())
assert_(not "HAL-9000".istitle())
assert_(" \t\r\n".isspace())
assert_(not "".isspace())
---
assert_eq("a".join("ctmrn".elems()), "catamaran")
assert_eq(list("abc".elems()), ["a", "b", "c"])
