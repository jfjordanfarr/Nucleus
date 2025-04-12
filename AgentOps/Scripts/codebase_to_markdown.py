import os
import argparse
import fnmatch
import re
from pathlib import Path

# --- Language Mappings (can be extended) ---
LANGUAGE_MAP = {
    '.py': 'python',
    '.js': 'javascript',
    '.ts': 'typescript',
    '.tsx': 'typescript',
    '.java': 'java',
    '.cs': 'csharp',
    '.fs': 'fsharp',
    '.vb': 'vbnet',
    '.go': 'go',
    '.rs': 'rust',
    '.c': 'c',
    '.cpp': 'cpp',
    '.h': 'cpp', # Often used with C/C++
    '.html': 'html',
    '.css': 'css',
    '.scss': 'scss',
    '.less': 'less',
    '.xml': 'xml',
    '.json': 'json',
    '.yaml': 'yaml',
    '.yml': 'yaml',
    '.sh': 'bash',
    '.bash': 'bash',
    '.zsh': 'bash',
    '.ps1': 'powershell',
    '.psm1': 'powershell',
    '.psd1': 'powershell',
    '.rb': 'ruby',
    '.php': 'php',
    '.sql': 'sql',
    '.md': 'markdown',
    '.txt': '', # Plain text
    '.gitignore': 'ignore',
    '.gitattributes': 'ignore',
    'Dockerfile': 'dockerfile',
    # Add more as needed
}

def get_language_hint(file_path):
    """Gets a language hint for Markdown code blocks based on file extension."""
    suffix = file_path.suffix.lower()
    if suffix in LANGUAGE_MAP:
        return LANGUAGE_MAP[suffix]
    # Handle files with no extension but specific names
    if file_path.name in LANGUAGE_MAP:
        return LANGUAGE_MAP[file_path.name]
    return '' # Default to plain text or no hint

def parse_gitignore(gitignore_path):
    """Parses a .gitignore file and returns a list of patterns."""
    patterns = []
    if not gitignore_path.is_file():
        return patterns
    try:
        with open(gitignore_path, 'r', encoding='utf-8', errors='ignore') as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith('#'):
                    patterns.append(line)
    except Exception as e:
        print(f"Warning: Could not read gitignore {gitignore_path}: {e}")
    return patterns

def get_gitignore_patterns(directory):
    """Finds all .gitignore files from the directory up to the root and returns patterns."""
    all_patterns = {} # Dict[Path, List[str]] storing patterns per gitignore file dir
    current_dir = Path(directory).resolve()
    root = Path(current_dir.anchor)

    while True:
        gitignore_file = current_dir / '.gitignore'
        if gitignore_file.is_file():
            if gitignore_file.parent not in all_patterns: # Avoid redundant parsing if nested
                 patterns = parse_gitignore(gitignore_file)
                 if patterns:
                     print(f"DEBUG: Found .gitignore: {gitignore_file} with {len(patterns)} patterns")
                     all_patterns[gitignore_file.parent] = patterns # Store patterns relative to their dir
        if current_dir == root:
            break
        parent = current_dir.parent
        if parent == current_dir: # Should not happen, but safeguard
             break
        current_dir = parent
    return all_patterns


def matches_gitignore(relative_path_posix, gitignore_patterns_map):
    """Checks if a relative path matches any gitignore patterns from relevant directories."""
    path_obj = Path(relative_path_posix)
    # Check patterns from parent directories downwards
    for gitignore_dir, patterns in gitignore_patterns_map.items():
        try:
            # Check if the file path is within or below the gitignore_dir
            # We need the relative path of the file *to the gitignore dir*
            relative_to_gitignore_dir = path_obj.relative_to(gitignore_dir.relative_to(Path('.').resolve())).as_posix()
        except ValueError:
            # The file is not under this gitignore's directory scope
            continue

        for pattern in patterns:
            # Basic fnmatch implementation (doesn't cover all gitignore nuances like !)
            # Handle directory matching (pattern ending with /)
            is_dir_pattern = pattern.endswith('/')
            match_pattern = pattern.rstrip('/')

            # A pattern without '/' can match a file or directory
            # A pattern with '/' only matches a directory
            # Need to check against the path string and potentially its parts

            # Simplistic matching for now:
            if fnmatch.fnmatch(relative_to_gitignore_dir, match_pattern) or \
               fnmatch.fnmatch(relative_to_gitignore_dir, match_pattern + '/*') or \
               any(fnmatch.fnmatch(part, match_pattern) for part in Path(relative_to_gitignore_dir).parts): # Match any segment
                # Refine directory matching
                if is_dir_pattern:
                     # Check if the match is actually for a directory segment
                     if Path(relative_to_gitignore_dir).is_dir() or (match_pattern in Path(relative_to_gitignore_dir).parts): # Heuristic
                         print(f"DEBUG: Gitignored (DIR pattern '{pattern}'): {relative_path_posix}")
                         return True
                else:
                    print(f"DEBUG: Gitignored (pattern '{pattern}'): {relative_path_posix}")
                    return True
    return False


def main():
    parser = argparse.ArgumentParser(description="Dump codebase structure and content to a Markdown file.")
    parser.add_argument("-s", "--source", required=True, help="Source directory to scan.")
    parser.add_argument("-o", "--output", required=True, help="Output Markdown file path.")
    parser.add_argument("-e", "--exclude", nargs='*', default=['.git', '.vscode', '.idea', 'bin', 'obj', 'node_modules'],
                        help="Directory names to exclude.")
    parser.add_argument("-f", "--force", action="store_true", help="Overwrite output file if it exists.")
    parser.add_argument("-v", "--verbose", action="store_true", help="Enable verbose output.")

    args = parser.parse_args()

    source_dir = Path(args.source).resolve()
    output_file = Path(args.output).resolve()
    exclude_dirs = set(args.exclude)
    verbose = args.verbose

    if not source_dir.is_dir():
        print(f"Error: Source directory '{source_dir}' not found or is not a directory.")
        return

    if output_file.exists() and not args.force:
        print(f"Error: Output file '{output_file}' already exists. Use --force to overwrite.")
        return

    # Ensure output directory exists
    output_file.parent.mkdir(parents=True, exist_ok=True)

    print(f"Source Directory: {source_dir}")
    print(f"Output File: {output_file}")
    print(f"Exclude Dirs: {', '.join(exclude_dirs)}")

    # --- Gitignore Handling ---
    print("Collecting .gitignore patterns...")
    gitignore_map = get_gitignore_patterns(source_dir)
    print(f"Found patterns in {len(gitignore_map)} .gitignore files.")

    processed_count = 0
    skipped_excluded_dir = 0
    skipped_gitignore = 0
    skipped_read_error = 0
    skipped_output_file = 0 # Added counter for the output file itself

    try:
        with open(output_file, 'w', encoding='utf-8') as md_file:
            md_file.write(f"# Code Dump: {source_dir.name}\n\n")
            md_file.write(f"*Generated on: {Path().cwd().name}*\n\n") # Simple timestamp marker

            for root, dirs, files in os.walk(source_dir, topdown=True):
                current_dir_path = Path(root)
                relative_dir_path = current_dir_path.relative_to(source_dir)

                # --- Directory Exclusion ---
                # Modify dirs in-place to prevent os.walk from descending
                dirs[:] = [d for d in dirs if d not in exclude_dirs and not d.startswith('.')] # Also exclude hidden dirs

                # Filter dirs based on gitignore (simplistic: check if dir name matches pattern)
                original_dirs = list(dirs) # Copy before filtering for verbose reporting
                dirs_to_remove_gitignore = set()
                for d in dirs:
                     dir_rel_path_posix = (relative_dir_path / d).as_posix().replace('\\', '/')
                     if matches_gitignore(dir_rel_path_posix, gitignore_map):
                         dirs_to_remove_gitignore.add(d)

                # Report skipped dirs
                skipped_now_excluded = set(original_dirs) - set(dirs)
                skipped_now_gitignore = dirs_to_remove_gitignore
                for skipped_dir in skipped_now_excluded:
                     if verbose: print(f"Skipping excluded directory: {(relative_dir_path / skipped_dir).as_posix()}")
                     skipped_excluded_dir += 1 # Rough count
                for skipped_dir in skipped_now_gitignore:
                     if verbose: print(f"Skipping gitignored directory: {(relative_dir_path / skipped_dir).as_posix()}")
                     skipped_gitignore += 1 # Rough count

                # Update dirs list for os.walk
                dirs[:] = [d for d in dirs if d not in dirs_to_remove_gitignore]


                # --- File Processing ---
                for filename in files:
                    file_path = current_dir_path / filename
                    relative_path = file_path.relative_to(source_dir)
                    relative_path_posix = relative_path.as_posix().replace('\\', '/') # Use forward slashes for gitignore

                    # --- Skip the output file itself ---
                    if file_path == output_file:
                        if verbose: print(f"Skipping output file itself: {relative_path_posix}")
                        skipped_output_file += 1
                        continue

                    # Check exclusions again (e.g. if a file is explicitly excluded)
                    if any(part in exclude_dirs for part in relative_path.parts):
                        if verbose: print(f"Skipping excluded file (in path): {relative_path_posix}")
                        skipped_excluded_dir +=1 # Count doesn't distinguish file/dir well here
                        continue

                    # Check gitignore
                    if matches_gitignore(relative_path_posix, gitignore_map):
                        if verbose: print(f"Skipping gitignored file: {relative_path_posix}")
                        skipped_gitignore += 1
                        continue

                    # Read and write content
                    if verbose: print(f"Processing file: {relative_path_posix}")
                    try:
                        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f_content:
                            content = f_content.read()

                        lang_hint = get_language_hint(file_path)
                        md_file.write(f"## {relative_path_posix}\n\n")
                        md_file.write(f"```{lang_hint}\n")
                        md_file.write(content)
                        md_file.write(f"\n```\n\n")
                        processed_count += 1

                    except Exception as e:
                        print(f"Warning: Could not read file {file_path}: {e}")
                        skipped_read_error += 1

    except Exception as e:
        print(f"Error writing to output file {output_file}: {e}")
        return

    print("\nScript finished.")
    print(f" - Files dumped: {processed_count}")
    total_skipped = skipped_excluded_dir + skipped_gitignore + skipped_read_error + skipped_output_file # Added output file skip to total
    print(f" - Total items skipped: {total_skipped}")
    print(f"   - Skipped (Excluded Dir/Path): {skipped_excluded_dir}")
    print(f"   - Skipped (.gitignore): {skipped_gitignore}")
    print(f"   - Skipped (Output File): {skipped_output_file}") # Added summary line
    print(f"   - Skipped (Read Error): {skipped_read_error}")
    print(f"Output written to: {output_file}")

if __name__ == "__main__":
    main()
