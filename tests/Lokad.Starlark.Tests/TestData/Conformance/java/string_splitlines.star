# Subset of test_suite/testdata/java string_splitlines coverage.

assert_eq("".splitlines(), [])
assert_eq("\n".splitlines(), [""])
assert_eq("\ntest".splitlines(), ["", "test"])
assert_eq("test\n".splitlines(), ["test"])
assert_eq("this\nis\na\ntest".splitlines(), ["this", "is", "a", "test"])
assert_eq("\n\n\n".splitlines(), ["", "", ""])
---
assert_eq("".splitlines(True), [])
assert_eq("\n".splitlines(True), ["\n"])
assert_eq("this\nis\na\ntest".splitlines(True), ["this\n", "is\n", "a\n", "test"])
assert_eq("\ntest".splitlines(True), ["\n", "test"])
assert_eq("test\n".splitlines(True), ["test\n"])
