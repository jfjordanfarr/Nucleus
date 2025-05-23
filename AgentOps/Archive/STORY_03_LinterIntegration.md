# Adding StyleCop and Refining Existing Editorconfig Rules

Date: 2025-04-06

The addition of StyleCop produced several rules which we opted to disable due to false positives or stylistic disagreements. The EditorConfig file looks like so at the time of writing:

```editorconfig
# EditorConfig helps maintain consistent coding styles for multiple developers
# See https://editorconfig.org/ for more information

# Top-most EditorConfig file
root = true

# Baseline settings for all files
[*]
charset = utf-8
end_of_line = crlf
indent_style = space
insert_final_newline = true
trim_trailing_whitespace = true

# C# files
[*.cs]
indent_size = 4
file_header_template = Copyright (c) 2025 Jordan Sterling Farr\nLicensed under the MIT license. See LICENSE file in the project root for full license information.\n

# CSharp code style settings:
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion
csharp_prefer_braces = when_multiline:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion
csharp_style_expression_bodied_constructors = when_on_single_line:suggestion
csharp_style_expression_bodied_operators = when_on_single_line:suggestion
csharp_style_expression_bodied_properties = when_on_single_line:suggestion
csharp_style_expression_bodied_indexers = when_on_single_line:suggestion
csharp_style_expression_bodied_accessors = when_on_single_line:suggestion
csharp_style_pattern_matching_over_is_with_cast_check = true:suggestion
csharp_style_pattern_matching_over_as_with_null_check = true:suggestion
csharp_style_inlined_variable_declaration = true:suggestion
csharp_style_throw_expression = true:suggestion
csharp_style_conditional_delegate_call = true:suggestion
csharp_prefer_simple_default_expression = true:suggestion

# Formatting rules
dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_space_after_cast = false
csharp_space_after_keywords_in_control_flow_statements = true
csharp_space_between_method_call_parameter_list_parentheses = false
csharp_space_between_method_declaration_parameter_list_parentheses = false
csharp_space_between_parentheses = false
csharp_preserve_single_line_statements = false
csharp_preserve_single_line_blocks = true

# .NET code quality settings
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
dotnet_style_readonly_field = true:suggestion
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
dotnet_style_prefer_auto_properties = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion
dotnet_style_prefer_conditional_expression_over_assignment = true:suggestion
dotnet_style_prefer_conditional_expression_over_return = true:suggestion


# Security-focused code analysis settings
dotnet_diagnostic.CA2100.severity = error  # Review SQL queries for security vulnerabilities
dotnet_diagnostic.CA5350.severity = error  # Do not use weak or broken cryptographic algorithms
dotnet_diagnostic.CA5351.severity = error  # Do not use broken cryptographic algorithms
dotnet_diagnostic.CA5358.severity = error  # Do not use unsafe cipher modes
dotnet_diagnostic.CA5364.severity = error  # Do not use deprecated security protocols
dotnet_diagnostic.CA5369.severity = error  # Use XmlReader for schema validation
dotnet_diagnostic.CA5377.severity = error  # Use container level access policy
dotnet_diagnostic.CA5379.severity = error  # Ensure key derivation function algorithm is sufficiently strong
dotnet_diagnostic.CA5381.severity = error  # Ensure certificates are not installed into the local certificate store
dotnet_diagnostic.CA5386.severity = error  # Avoid hardcoding SecurityProtocolType
dotnet_diagnostic.CA5394.severity = error  # Do not use insecure randomness
dotnet_diagnostic.CA2119.severity = error  # Seal methods that satisfy private interfaces

# Code Quality - Access Modifiers and Encapsulation (similar to SonarLint rules)
dotnet_diagnostic.CA1051.severity = warning  # Do not declare visible instance fields
dotnet_diagnostic.CA2227.severity = warning  # Collection properties should be read only
dotnet_diagnostic.CA1819.severity = warning  # Properties should not return arrays
dotnet_diagnostic.CA1720.severity = warning  # Identifiers should not contain type names
dotnet_diagnostic.CA1024.severity = suggestion  # Use properties where appropriate
dotnet_diagnostic.CA1031.severity = warning  # Do not catch general exception types
dotnet_diagnostic.CA2000.severity = warning  # Dispose objects before losing scope

# Code Quality - Unused Code Detection (similar to SonarLint rules)
dotnet_diagnostic.CA1801.severity = warning  # Review unused parameters
dotnet_diagnostic.CA1804.severity = warning  # Remove unused locals
dotnet_diagnostic.CA1823.severity = warning  # Avoid unused private fields
dotnet_diagnostic.CA1812.severity = warning  # Avoid uninstantiated internal classes
dotnet_diagnostic.CA1822.severity = warning  # Mark members as static

# Code Quality - Method Organization (similar to SonarLint rules)
dotnet_diagnostic.CA1062.severity = warning  # Validate arguments of public methods
dotnet_diagnostic.CA1508.severity = warning  # Avoid dead conditional code
dotnet_diagnostic.CA1822.severity = warning  # Mark members as static when appropriate
dotnet_diagnostic.CA1725.severity = suggestion  # Parameter names should match base declaration
dotnet_diagnostic.CA1716.severity = suggestion  # Identifiers should not match keywords
dotnet_diagnostic.CA1054.severity = warning  # URI parameters should not be strings
dotnet_diagnostic.CA1056.severity = warning  # URI properties should not be strings
dotnet_diagnostic.CA1055.severity = warning  # URI return values should not be strings

# AI-Assisted Development Patterns - Nullable Reference Types
dotnet_diagnostic.CS8600.severity = warning  # Converting null literal or possible null value to non-nullable type
dotnet_diagnostic.CS8602.severity = warning  # Dereference of a possibly null reference
dotnet_diagnostic.CS8603.severity = warning  # Possible null reference return
dotnet_diagnostic.CS8604.severity = warning  # Possible null reference argument
dotnet_diagnostic.CS8618.severity = warning  # Non-nullable field must contain a non-null value when exiting constructor
dotnet_diagnostic.CS8625.severity = warning  # Cannot convert null literal to non-nullable reference type
dotnet_diagnostic.CA1062.severity = warning  # Validate parameters for null (when nullable annotations aren't used)

# AI-Assisted Development Patterns - Resource Management
dotnet_diagnostic.CA2000.severity = warning  # Dispose objects before losing scope
dotnet_diagnostic.CA2213.severity = warning  # Disposable fields should be disposed
dotnet_diagnostic.CA1063.severity = warning  # Implement IDisposable correctly
dotnet_diagnostic.CA1816.severity = suggestion  # Call GC.SuppressFinalize correctly
dotnet_diagnostic.SYSLIB0014.severity = warning  # Type or member is obsolete (like WebClient)

# AI-Assisted Development Patterns - Async/Await
dotnet_diagnostic.CA1842.severity = warning  # Do not use 'WhenAll' with a single task
dotnet_diagnostic.CA1843.severity = warning  # Do not use 'WaitAll' with a single task
dotnet_diagnostic.CA2007.severity = suggestion  # Do not directly await a Task
dotnet_diagnostic.CA2008.severity = warning  # Do not create tasks without passing a TaskScheduler
dotnet_diagnostic.CA2009.severity = warning  # Do not call ToImmutableCollection on an ImmutableCollection
dotnet_diagnostic.CA2012.severity = warning  # Use ValueTasks correctly

# AI-Assisted Development Patterns - Modern C# Features
dotnet_diagnostic.IDE0041.severity = warning  # Use 'is null' check
dotnet_diagnostic.IDE0083.severity = suggestion  # Use pattern matching ('is not null' instead of '!= null')
dotnet_diagnostic.IDE0090.severity = suggestion  # Use 'new()' for object/collection initialization
dotnet_diagnostic.IDE0066.severity = suggestion  # Use switch expression
dotnet_diagnostic.IDE0078.severity = suggestion  # Use pattern matching
dotnet_diagnostic.CA1847.severity = suggestion  # Use string.Contains(char) instead of string.Contains(string)
dotnet_diagnostic.CA1845.severity = suggestion  # Use span-based 'string.Concat'
dotnet_diagnostic.CA1860.severity = suggestion  # Prefer comparing 'Count' to 0 rather than using 'Any()'

# AI-Assisted Development Patterns - API Design (critical for generating good code patterns)
dotnet_diagnostic.CA1032.severity = warning  # Implement standard exception constructors
dotnet_diagnostic.CA1034.severity = suggestion  # Nested types should not be visible
dotnet_diagnostic.CA1000.severity = suggestion  # Do not declare static members on generic types
dotnet_diagnostic.CA1036.severity = suggestion  # Override methods on comparable types
dotnet_diagnostic.CA1040.severity = suggestion  # Avoid empty interfaces
dotnet_diagnostic.CA1724.severity = warning  # Type names should not match namespaces
dotnet_diagnostic.CA1711.severity = suggestion  # Identifiers should not have incorrect suffix

# Azure and Cosmos DB Patterns (especially relevant for your Nucleus-OmniRAG project)
dotnet_diagnostic.CA2252.severity = warning  # Prefers StringComparison overloads
dotnet_diagnostic.CA1834.severity = suggestion  # Use StringBuilder.Append(char) for single character strings
dotnet_diagnostic.CA1310.severity = warning  # Specify StringComparison for correctness (important for cloud apps)
dotnet_diagnostic.CA2258.severity = warning  # Avoid calling Contains with a substring of length 1
dotnet_diagnostic.CA2259.severity = warning  # Avoid thread culture-dependent string methods
dotnet_diagnostic.CA2201.severity = warning  # Do not raise reserved exception types
dotnet_diagnostic.CA2016.severity = warning  # Forward the 'CancellationToken' parameter (critical for Cosmos DB operations)

# Custom / External Analyzer Settings
dotnet_diagnostic.SA1107.severity = warning # Flag TODO comments (requires StyleCop.Analyzers NuGet package)

# --- StyleCop Rules Configuration ---

# Rules to Keep as Warning (Enforce)
dotnet_diagnostic.SA1600.severity = warning  # Elements should be documented

# Rules to Relax (Suggestions)
dotnet_diagnostic.SA1201.severity = suggestion # Element order (e.g., constructor position)
dotnet_diagnostic.SA1512.severity = suggestion # Single-line comment position
dotnet_diagnostic.SA1516.severity = suggestion # Elements should be separated by blank line
dotnet_diagnostic.SA1117.severity = suggestion # Parameters should be on separate lines
dotnet_diagnostic.SA1413.severity = suggestion # Use trailing comma in multi-line initializers
dotnet_diagnostic.SA1505.severity = suggestion # Opening brace should not be followed by blank line

# Rules to Disable (Personal Preference / Conflict)
dotnet_diagnostic.SA1000.severity = none     # Keyword 'new' should be followed by a space
dotnet_diagnostic.SA1009.severity = none     # No space before closing parenthesis
dotnet_diagnostic.SA1010.severity = none     # Opening square bracket preceded by space
dotnet_diagnostic.SA1025.severity = none     # Don't use 'this.' unnecessarily
dotnet_diagnostic.SA1028.severity = none     # No trailing whitespace
dotnet_diagnostic.SA1101.severity = none     # Do not require 'this.' prefix
dotnet_diagnostic.SA1108.severity = none     # Block statements should not contain embedded contents
dotnet_diagnostic.SA1111.severity = none     # Closing parenthesis on line of last parameter
dotnet_diagnostic.SA1116.severity = none     # Parameters should start on new line
dotnet_diagnostic.SA1133.severity = none     # Allow multiple attributes to be placed in one set of square brackets.
dotnet_diagnostic.SA1200.severity = none     # Allow using directives outside namespace
dotnet_diagnostic.SA1210.severity = none     # Using directives should be ordered alphabetically by the namespaces
dotnet_diagnostic.SA1309.severity = none     # Allow field names starting with _
dotnet_diagnostic.SA1313.severity = none     # Allow PascalCase parameters (common in records)
dotnet_diagnostic.SA1402.severity = none     # Allow a single file to contain multiple types
dotnet_diagnostic.SA1500.severity = none     # Allow braces to be omitted
dotnet_diagnostic.SA1503.severity = none     # Allow braces to be omitted
dotnet_diagnostic.SA1507.severity = none     # Allow multiple blank lines
dotnet_diagnostic.SA1513.severity = none     # Allow closing brace to be followed by blank line
dotnet_diagnostic.SA1515.severity = none     # Allow single-line comment to be preceded by blank line
dotnet_diagnostic.SA1633.severity = none     # File header required (matches existing template)

# IDE0005: Using directive is unnecessary.
dotnet_diagnostic.IDE0005.severity = suggestion
ty = none     # Allow closing brace to be followed by blank line

# XML project files, JSON files
[*.{csproj,json,xml,yml,yaml}]
indent_size = 2

# Markdown files
[*.md]
trim_trailing_whitespace = false
