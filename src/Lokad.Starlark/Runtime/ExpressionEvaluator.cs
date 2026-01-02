using Lokad.Starlark.Syntax;

namespace Lokad.Starlark.Runtime;

public sealed class ExpressionEvaluator
{
    public StarlarkValue Evaluate(Expression expression, StarlarkEnvironment environment)
    {
        return expression switch
        {
            LiteralExpression literal => ConvertLiteral(literal.Value),
            IdentifierExpression identifier => ResolveIdentifier(identifier, environment),
            UnaryExpression unary => EvaluateUnary(unary, environment),
            BinaryExpression binary => EvaluateBinary(binary, environment),
            CallExpression call => EvaluateCall(call, environment),
            _ => throw new ArgumentOutOfRangeException(nameof(expression), expression, "Unsupported expression.")
        };
    }

    private static StarlarkValue ConvertLiteral(object value)
    {
        return value switch
        {
            null => StarlarkNone.Instance,
            bool b => new StarlarkBool(b),
            long l => new StarlarkInt(l),
            int i => new StarlarkInt(i),
            double d => new StarlarkFloat(d),
            float f => new StarlarkFloat(f),
            string s => new StarlarkString(s),
            _ => throw new InvalidOperationException(
                $"Unsupported literal type: {value.GetType().Name}.")
        };
    }

    private static StarlarkValue ResolveIdentifier(IdentifierExpression identifier, StarlarkEnvironment environment)
    {
        if (!environment.Globals.TryGetValue(identifier.Name, out var value))
        {
            throw new KeyNotFoundException($"Undefined identifier '{identifier.Name}'.");
        }

        return value;
    }

    private StarlarkValue EvaluateUnary(UnaryExpression unary, StarlarkEnvironment environment)
    {
        var operand = Evaluate(unary.Operand, environment);

        return unary.Operator switch
        {
            UnaryOperator.Not => new StarlarkBool(!operand.IsTruthy),
            UnaryOperator.Negate => Negate(operand),
            _ => throw new ArgumentOutOfRangeException(nameof(unary.Operator), unary.Operator, null)
        };
    }

    private StarlarkValue Negate(StarlarkValue operand)
    {
        return operand switch
        {
            StarlarkInt value => new StarlarkInt(-value.Value),
            StarlarkFloat value => new StarlarkFloat(-value.Value),
            _ => throw new InvalidOperationException(
                $"Unary '-' not supported for type '{operand.TypeName}'.")
        };
    }

    private StarlarkValue EvaluateBinary(BinaryExpression binary, StarlarkEnvironment environment)
    {
        if (binary.Operator == BinaryOperator.And)
        {
            var left = Evaluate(binary.Left, environment);
            return left.IsTruthy ? Evaluate(binary.Right, environment) : left;
        }

        if (binary.Operator == BinaryOperator.Or)
        {
            var left = Evaluate(binary.Left, environment);
            return left.IsTruthy ? left : Evaluate(binary.Right, environment);
        }

        var leftValue = Evaluate(binary.Left, environment);
        var rightValue = Evaluate(binary.Right, environment);

        return binary.Operator switch
        {
            BinaryOperator.Add => Add(leftValue, rightValue),
            BinaryOperator.Subtract => Subtract(leftValue, rightValue),
            BinaryOperator.Multiply => Multiply(leftValue, rightValue),
            BinaryOperator.Divide => Divide(leftValue, rightValue),
            BinaryOperator.Equal => new StarlarkBool(Equals(leftValue, rightValue)),
            BinaryOperator.NotEqual => new StarlarkBool(!Equals(leftValue, rightValue)),
            _ => throw new ArgumentOutOfRangeException(nameof(binary.Operator), binary.Operator, null)
        };
    }

    private StarlarkValue EvaluateCall(CallExpression call, StarlarkEnvironment environment)
    {
        var callee = Evaluate(call.Callee, environment);
        if (callee is not StarlarkFunction function)
        {
            throw new InvalidOperationException(
                $"Attempted to call non-callable value of type '{callee.TypeName}'.");
        }

        var args = new StarlarkValue[call.Arguments.Count];
        for (var i = 0; i < call.Arguments.Count; i++)
        {
            args[i] = Evaluate(call.Arguments[i], environment);
        }

        return function.Call(args);
    }

    private static StarlarkValue Add(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkString leftString && right is StarlarkString rightString)
        {
            return new StarlarkString(leftString.Value + rightString.Value);
        }

        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value + rightInt.Value);
        }

        if (TryGetNumber(left, out var leftNumber, out var leftIsInt)
            && TryGetNumber(right, out var rightNumber, out var rightIsInt))
        {
            return leftIsInt && rightIsInt
                ? new StarlarkInt((long)(leftNumber + rightNumber))
                : new StarlarkFloat(leftNumber + rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '+' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue Subtract(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value - rightInt.Value);
        }

        if (TryGetNumber(left, out var leftNumber, out var leftIsInt)
            && TryGetNumber(right, out var rightNumber, out var rightIsInt))
        {
            return leftIsInt && rightIsInt
                ? new StarlarkInt((long)(leftNumber - rightNumber))
                : new StarlarkFloat(leftNumber - rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '-' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue Multiply(StarlarkValue left, StarlarkValue right)
    {
        if (left is StarlarkInt leftInt && right is StarlarkInt rightInt)
        {
            return new StarlarkInt(leftInt.Value * rightInt.Value);
        }

        if (TryGetNumber(left, out var leftNumber, out var leftIsInt)
            && TryGetNumber(right, out var rightNumber, out var rightIsInt))
        {
            return leftIsInt && rightIsInt
                ? new StarlarkInt((long)(leftNumber * rightNumber))
                : new StarlarkFloat(leftNumber * rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '*' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static StarlarkValue Divide(StarlarkValue left, StarlarkValue right)
    {
        if (TryGetNumber(left, out var leftNumber, out _)
            && TryGetNumber(right, out var rightNumber, out _))
        {
            return new StarlarkFloat(leftNumber / rightNumber);
        }

        throw new InvalidOperationException(
            $"Operator '/' not supported for '{left.TypeName}' and '{right.TypeName}'.");
    }

    private static bool TryGetNumber(StarlarkValue value, out double number, out bool isInt)
    {
        switch (value)
        {
            case StarlarkInt intValue:
                number = intValue.Value;
                isInt = true;
                return true;
            case StarlarkFloat floatValue:
                number = floatValue.Value;
                isInt = false;
                return true;
            default:
                number = 0;
                isInt = false;
                return false;
        }
    }
}
