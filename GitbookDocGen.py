import argparse
import os
import re
import sys
from dataclasses import dataclass, field
from typing import List, Optional, Dict, Tuple

MODIFIER_WORDS = {
    "public", "private", "protected", "internal", "static", "virtual",
    "override", "abstract", "sealed", "async", "readonly", "const", "new",
    "extern", "unsafe", "partial", "volatile", "event", "in", "out", "ref",
}

NON_PUBLIC_MODIFIERS = {"private", "protected", "internal"}

TYPE_KEYWORDS = {"class", "struct", "interface", "enum"}


def _skip_raw_string(text: str, i: int) -> int:
    n = len(text)
    q = 0
    while i + q < n and text[i + q] == '"':
        q += 1
    fence = '"' * q
    j = i + q
    while j < n:
        k = text.find(fence, j)
        if k == -1:
            return n
        e = k
        while e < n and text[e] == '"':
            e += 1
        if e - k >= q:
            return e
        j = e
    return n


def _skip_interpolation_hole(text: str, j: int) -> int:
    n = len(text)
    depth = 0
    i = j
    while i < n:
        c = text[i]
        if c == '"':
            i = skip_string(text, i)
            continue
        if c == "'":
            i = skip_char(text, i)
            continue
        if c == '{':
            depth += 1
            i += 1
            continue
        if c == '}':
            depth -= 1
            i += 1
            if depth == 0:
                return i
            continue
        i += 1
    return n


def skip_string(text: str, i: int) -> int:
    n = len(text)
    prefix = ""
    k = i - 1
    while k >= 0 and text[k] in '@$':
        prefix = text[k] + prefix
        k -= 1
    verbatim = '@' in prefix
    interpolated = '$' in prefix

    if text.startswith('"""', i):
        return _skip_raw_string(text, i)

    j = i + 1
    while j < n:
        c = text[j]
        if interpolated and c == '{':
            if j + 1 < n and text[j + 1] == '{':
                j += 2
                continue
            j = _skip_interpolation_hole(text, j)
            continue
        if interpolated and c == '}' and j + 1 < n and text[j + 1] == '}':
            j += 2
            continue
        if verbatim:
            if c == '"':
                if j + 1 < n and text[j + 1] == '"':
                    j += 2
                    continue
                return j + 1
            j += 1
            continue
        if c == '\\':
            j += 2
            continue
        if c == '"':
            return j + 1
        if c == '\n':
            return j
        j += 1
    return n


def skip_char(text: str, i: int) -> int:
    n = len(text)
    j = i + 1
    if j < n and text[j] == '\\':
        j += 2
    else:
        j += 1
    if j < n and text[j] == "'":
        j += 1
    return j


def find_matching_brace(text: str, start: int) -> int:
    assert text[start] == '{'
    n = len(text)
    depth = 0
    i = start
    while i < n:
        c = text[i]
        if c == '"':
            i = skip_string(text, i)
            continue
        if c == "'":
            i = skip_char(text, i)
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '/':
            j = text.find('\n', i)
            i = j if j != -1 else n
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '*':
            j = text.find('*/', i + 2)
            i = j + 2 if j != -1 else n
            continue
        if c == '{':
            depth += 1
            i += 1
            continue
        if c == '}':
            depth -= 1
            i += 1
            if depth == 0:
                return i - 1
            continue
        i += 1
    return -1


@dataclass
class XmlDoc:
    summary: str = ""
    remarks: str = ""
    returns: str = ""
    value: str = ""
    params: "Dict[str, str]" = field(default_factory=dict)
    exceptions: "Dict[str, str]" = field(default_factory=dict)
    inherit: bool = False
    inherit_cref: str = ""

    def is_empty(self) -> bool:
        return not (self.summary or self.remarks or self.returns or self.value
                    or self.params or self.exceptions)

    def merge_from(self, other: "XmlDoc") -> None:
        if not self.summary:
            self.summary = other.summary
        if not self.remarks:
            self.remarks = other.remarks
        if not self.returns:
            self.returns = other.returns
        if not self.value:
            self.value = other.value
        for k, v in other.params.items():
            self.params.setdefault(k, v)
        for k, v in other.exceptions.items():
            self.exceptions.setdefault(k, v)


def parse_xmldoc(raw_lines: List[str]) -> XmlDoc:
    joined = "\n".join(raw_lines).strip()
    doc = XmlDoc()
    if not joined:
        return doc

    def extract(tag: str, txt: str) -> Optional[str]:
        m = re.search(rf"<{tag}[^>]*>(.*?)</{tag}>", txt, re.DOTALL | re.IGNORECASE)
        if m:
            return clean_doc_text(m.group(1))
        return None

    m_inherit = re.search(r'<inheritdoc\s*(?:cref="([^"]*)")?\s*/?>', joined, re.IGNORECASE)
    if m_inherit:
        doc.inherit = True
        doc.inherit_cref = m_inherit.group(1) or ""
        joined = joined[:m_inherit.start()] + joined[m_inherit.end():]

    summary = extract("summary", joined)
    if summary:
        doc.summary = summary
    remarks = extract("remarks", joined)
    if remarks:
        doc.remarks = remarks
    returns = extract("returns", joined)
    if returns:
        doc.returns = returns

    for m in re.finditer(
        r'<param\s+name="([^"]+)"\s*/?>(.*?)(?:</param>|(?=<param)|$)',
        joined, re.DOTALL | re.IGNORECASE,
    ):
        doc.params[m.group(1)] = clean_doc_text(m.group(2))

    for m in re.finditer(
        r'<exception\s+cref="([^"]+)"\s*/?>(.*?)(?:</exception>|(?=<exception)|$)',
        joined, re.DOTALL | re.IGNORECASE,
    ):
        cref = m.group(1)
        cref = re.sub(r'^[TMPF]:', '', cref).split('(')[0].split('.')[-1]
        doc.exceptions[cref] = clean_doc_text(m.group(2))

    val = extract("value", joined)
    if val:
        doc.value = val

    if doc.is_empty() and not doc.inherit:
        doc.summary = clean_doc_text(joined)
    return doc


def clean_doc_text(text: str) -> str:
    text = re.sub(r'<paramref\s+name="([^"]+)"\s*/?>', r'`\1`', text)
    text = re.sub(r'<see\s+cref="[^"]*?[.:]?([A-Za-z0-9_]+)"\s*/?>', r'`\1`', text)
    text = re.sub(r'<see\s+langword="([^"]+)"\s*/?>', r'`\1`', text)
    text = re.sub(r'<c>(.*?)</c>', r'`\1`', text, flags=re.DOTALL)
    text = re.sub(r'<code>(.*?)</code>', r'`\1`', text, flags=re.DOTALL)
    text = re.sub(r'</para>\s*|<br\s*/?>', '\n\n', text, flags=re.IGNORECASE)
    text = re.sub(r'<[^>]+>', '', text)

    out: List[str] = []
    buf: List[str] = []

    def flush() -> None:
        if buf:
            out.append(" ".join(buf))
            buf.clear()

    for raw in text.splitlines():
        ln = raw.strip()
        if not ln:
            flush()
            if out and out[-1] != "":
                out.append("")
            continue
        if re.match(r'^([-*+]|\d+\.)\s+', ln):
            flush()
            if out and out[-1] != "" and not re.match(r'^([-*+]|\d+\.)\s+', out[-1]):
                out.append("")
            out.append(ln)
            continue
        if out and re.match(r'^([-*+]|\d+\.)\s+', out[-1]) and not buf:
            out[-1] += " " + ln
            continue
        buf.append(ln)
    flush()

    while out and out[-1] == "":
        out.pop()
    return "\n".join(out).strip()


@dataclass
class Param:
    type: str
    name: str
    default: Optional[str] = None

    def render(self) -> str:
        s = f"{self.type} {self.name}".strip()
        if self.default:
            s += f" = {self.default}"
        return s


@dataclass
class Member:
    kind: str
    name: str
    type: str = ""
    params: "List[Param]" = field(default_factory=list)
    modifiers: "List[str]" = field(default_factory=list)
    doc: XmlDoc = field(default_factory=XmlDoc)
    enum_value: Optional[str] = None
    obsolete: bool = False
    is_static: bool = False
    accessors: str = ""

    def signature(self) -> str:
        if self.kind in ("method", "constructor"):
            params = ", ".join(p.render() for p in self.params)
            prefix = f"{self.type} ".lstrip() if self.type else ""
            return f"{prefix}{self.name}({params})".strip()
        if self.kind == "property":
            return f"{self.type} {self.name}"
        if self.kind == "field":
            return f"{self.type} {self.name}"
        if self.kind == "enumvalue":
            return self.name
        return self.name


@dataclass
class CSharpType:
    kind: str
    name: str
    generics: str = ""
    bases: str = ""
    gitbook_path: str = ""
    namespace: str = ""
    doc: XmlDoc = field(default_factory=XmlDoc)
    properties: "List[Member]" = field(default_factory=list)
    fields: "List[Member]" = field(default_factory=list)
    methods: "List[Member]" = field(default_factory=list)
    constructors: "List[Member]" = field(default_factory=list)
    enum_values: "List[Member]" = field(default_factory=list)
    source_file: str = ""

    @property
    def full_name(self) -> str:
        return f"{self.name}{self.generics}"


ATTR_RE = re.compile(
    r'\[\s*(?:[\w.]+\.)?GitBookPage(?:Attribute)?\s*\(\s*"(?P<path>(?:[^"\\]|\\.)*)"\s*\)\s*\]'
)

TYPE_HEADER_RE = re.compile(
    r'\s*(?:\[[^\[\]]*\]\s*)*'
    r'(?P<modifiers>(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|ref|unsafe|new)\s+)*)'
    r'(?P<kind>class|interface|struct|enum)\s+'
    r'(?P<name>[A-Za-z_]\w*)'
    r'(?P<generics><[^{}]*?>)?'
    r'(?P<tail>[^{]*)'
    r'\{',
    re.MULTILINE,
)


def split_bases_and_constraints(tail: str) -> str:
    t = tail.strip()
    if not t:
        return ""
    if t.startswith(':'):
        t = t[1:].strip()
    elif re.match(r'^where\b', t):
        return ""
    else:
        return ""
    m = re.search(r'\bwhere\b', t)
    if m:
        t = t[:m.start()]
    return t.strip().rstrip(',').strip()


NAMESPACE_RE = re.compile(r'namespace\s+([\w.]+)\s*[{;]')


def find_preceding_doc_comment(text: str, pos: int) -> XmlDoc:
    i = pos
    lines_before = text[:i].splitlines()
    collected: List[str] = []
    idx = len(lines_before) - 1
    while idx >= 0 and lines_before[idx].strip() == "":
        idx -= 1
    while idx >= 0 and lines_before[idx].strip().startswith("["):
        idx -= 1
        while idx >= 0 and lines_before[idx].strip() == "":
            idx -= 1
    while idx >= 0 and lines_before[idx].strip().startswith("///"):
        collected.append(re.sub(r'^\s*///\s?', '', lines_before[idx]))
        idx -= 1
    collected.reverse()
    return parse_xmldoc(collected)


def find_namespace_for(text: str, pos: int) -> str:
    ns = ""
    for m in NAMESPACE_RE.finditer(text, 0, pos):
        ns = m.group(1)
    return ns


def find_gitbook_types(text: str, source_file: str = "") -> List[CSharpType]:
    results: List[CSharpType] = []
    for attr_match in ATTR_RE.finditer(text):
        path = attr_match.group("path")
        search_from = attr_match.end()
        header_match = TYPE_HEADER_RE.match(text, search_from)
        if not header_match:
            continue

        brace_open = header_match.end() - 1
        brace_close = find_matching_brace(text, brace_open)
        if brace_close == -1:
            print(f"warning: unbalanced braces in {source_file or '<input>'}: could "
                  f"not find the end of '{header_match.group('name')}'; skipping.",
                  file=sys.stderr)
            continue

        body = text[brace_open + 1:brace_close]

        ctype = CSharpType(
            kind=header_match.group("kind"),
            name=header_match.group("name"),
            generics=header_match.group("generics") or "",
            bases=split_bases_and_constraints(header_match.group("tail") or ""),
            gitbook_path=path,
            namespace=find_namespace_for(text, attr_match.start()),
            doc=find_preceding_doc_comment(text, attr_match.start()),
            source_file=source_file,
        )

        if ctype.kind == "enum":
            ctype.enum_values = parse_enum_body(body)
        else:
            parse_type_body(body, ctype)

        results.append(ctype)
    return results


def split_top_level(text: str, sep: str) -> List[str]:
    parts = []
    depth = 0
    angle_depth = 0
    buf = []
    i = 0
    n = len(text)
    while i < n:
        c = text[i]
        if c == '"':
            j = skip_string(text, i)
            buf.append(text[i:j])
            i = j
            continue
        if c == "'":
            j = skip_char(text, i)
            buf.append(text[i:j])
            i = j
            continue
        if c in "([{":
            depth += 1
        elif c in ")]}":
            depth -= 1
        elif c == '<':
            angle_depth += 1
        elif c == '>':
            if angle_depth > 0:
                angle_depth -= 1
        if c == sep and depth == 0 and angle_depth == 0:
            parts.append("".join(buf))
            buf = []
            i += 1
            continue
        buf.append(c)
        i += 1
    parts.append("".join(buf))
    return parts


def parse_enum_body(body: str) -> List[Member]:
    entries = split_top_level(body, ',')
    members = []
    for entry in entries:
        entry_lines = entry.splitlines()
        doc_lines = []
        obsolete = False
        code_lines = []
        for ln in entry_lines:
            stripped = ln.strip()
            if stripped.startswith('///'):
                doc_lines.append(re.sub(r'^\s*///\s?', '', ln))
            elif stripped.startswith('//'):
                continue
            else:
                code_lines.append(ln)
        code = "\n".join(code_lines).strip()
        if not code:
            continue
        while code.startswith('['):
            close = code.find(']')
            if close == -1:
                break
            attr_text = code[:close + 1]
            if 'Obsolete' in attr_text:
                obsolete = True
            code = code[close + 1:].strip()
        if not code:
            continue
        if '=' in code:
            name_part, value_part = code.split('=', 1)
            name = name_part.strip()
            value = value_part.strip()
        else:
            name = code.strip()
            value = None
        name = name.strip().rstrip(',').strip()
        if not re.match(r'^[A-Za-z_]\w*$', name):
            continue
        members.append(Member(
            kind='enumvalue',
            name=name,
            enum_value=value,
            doc=parse_xmldoc(doc_lines),
            obsolete=obsolete,
        ))
    return members


def parse_type_body(body: str, ctype: CSharpType) -> None:
    n = len(body)
    i = 0
    pending_doc_lines: List[str] = []
    pending_obsolete = False

    is_interface = ctype.kind == "interface"

    while i < n:
        c = body[i]

        if c in ' \t\r\n':
            i += 1
            continue

        if c == '#':
            j = body.find('\n', i)
            i = (j + 1) if j != -1 else n
            continue

        if body.startswith('///', i):
            j = body.find('\n', i)
            line = body[i:j] if j != -1 else body[i:]
            pending_doc_lines.append(re.sub(r'^\s*///\s?', '', line))
            i = (j + 1) if j != -1 else n
            continue

        if c == '/' and i + 1 < n and body[i + 1] == '/':
            j = body.find('\n', i)
            i = (j + 1) if j != -1 else n
            continue

        if c == '/' and i + 1 < n and body[i + 1] == '*':
            j = body.find('*/', i + 2)
            i = (j + 2) if j != -1 else n
            continue

        if c == '[':
            depth = 1
            j = i + 1
            while j < n and depth > 0:
                if body[j] == '[':
                    depth += 1
                elif body[j] == ']':
                    depth -= 1
                j += 1
            attr_text = body[i:j]
            if 'Obsolete' in attr_text:
                pending_obsolete = True
            i = j
            continue

        nested_header = TYPE_HEADER_RE.match(body, i)
        if nested_header and body[i:nested_header.start('kind')].strip(
        ).replace('\n', ' ') == nested_header.group('modifiers').strip():
            brace_open = nested_header.end() - 1
            brace_close = find_matching_brace(body, brace_open)
            if brace_close != -1:
                i = brace_close + 1
                pending_doc_lines = []
                pending_obsolete = False
                continue

        start = i
        depth_paren = 0
        depth_brack = 0
        j = i
        terminator = None
        while j < n:
            cj = body[j]
            if cj == '"':
                j = skip_string(body, j)
                continue
            if cj == "'":
                j = skip_char(body, j)
                continue
            if cj == '/' and j + 1 < n and body[j + 1] == '/':
                nl = body.find('\n', j)
                j = nl if nl != -1 else n
                continue
            if cj == '/' and j + 1 < n and body[j + 1] == '*':
                cl = body.find('*/', j + 2)
                j = (cl + 2) if cl != -1 else n
                continue
            if cj == '(':
                depth_paren += 1
            elif cj == ')':
                depth_paren -= 1
            elif cj == '[':
                depth_brack += 1
            elif cj == ']':
                depth_brack -= 1
            elif cj == '{' and depth_paren <= 0 and depth_brack <= 0:
                terminator = '{'
                break
            elif cj == ';' and depth_paren <= 0 and depth_brack <= 0:
                terminator = ';'
                break
            j += 1

        if terminator is None:
            print(f"warning: {ctype.name}: stopped parsing members early "
                  f"(unterminated declaration).", file=sys.stderr)
            break

        header = body[start:j].strip()

        if terminator == '{':
            close = find_matching_brace(body, j)
            if close == -1:
                print(f"warning: {ctype.name}: unbalanced braces in a member "
                      f"body; remaining members skipped.", file=sys.stderr)
                break
            member_body = body[j + 1:close]
            i = close + 1
        else:
            member_body = None
            i = j + 1

        doc = parse_xmldoc(pending_doc_lines)
        obsolete = pending_obsolete
        pending_doc_lines = []
        pending_obsolete = False

        if not header:
            continue

        member = classify_member(header, member_body, ctype, is_interface)
        if member is None:
            continue

        if member_body is not None and not isinstance(member, list) \
                and member.kind == 'property':
            member.accessors = _read_accessors(member_body)

        for m in (member if isinstance(member, list) else [member]):
            m.doc = doc
            m.obsolete = obsolete
            _store_member(ctype, m)


ACCESSOR_RE = re.compile(
    r'(?:^|[;{}])\s*((?:private|protected|internal|public)\s+)?(get|set|init)\s*(?=[;{=])')


def _read_accessors(member_body: str) -> str:
    found = []
    for m in ACCESSOR_RE.finditer(member_body):
        vis = (m.group(1) or "").strip()
        acc = m.group(2)
        label = f"{vis} {acc}".strip()
        if label not in found:
            found.append(label)
    return "; ".join(found) + (";" if found else "")


def _store_member(ctype: CSharpType, m: Member) -> None:
    if m.kind == 'property':
        ctype.properties.append(m)
    elif m.kind == 'field':
        ctype.fields.append(m)
    elif m.kind == 'constructor':
        ctype.constructors.append(m)
    elif m.kind == 'method':
        ctype.methods.append(m)


def _leading_modifiers(text: str) -> Tuple[List[str], str]:
    mods = []
    rest = text
    while True:
        m = re.match(r'^\s*([A-Za-z_]+)\s+', rest)
        if not m or m.group(1) not in MODIFIER_WORDS:
            break
        mods.append(m.group(1))
        rest = rest[m.end():]
    return mods, rest


def _is_public(mods: List[str], is_interface: bool) -> bool:
    if is_interface:
        return not any(m in NON_PUBLIC_MODIFIERS for m in mods)
    if 'public' in mods:
        return True
    return False


def _find_top_level(text: str, target: str) -> int:
    depth = 0
    i = 0
    n = len(text)
    while i < n:
        c = text[i]
        if c == '"':
            i = skip_string(text, i)
            continue
        if c == "'":
            i = skip_char(text, i)
            continue
        cur_depth = depth
        if c in '([':
            depth += 1
        elif c in ')]':
            depth -= 1
        if c == target and cur_depth == 0:
            return i
        i += 1
    return -1


def find_first_assign_or_arrow(text: str) -> Tuple[Optional[str], int]:
    depth = 0
    i = 0
    n = len(text)
    while i < n:
        c = text[i]
        if c == '"':
            i = skip_string(text, i)
            continue
        if c == "'":
            i = skip_char(text, i)
            continue
        if c in '([':
            depth += 1
            i += 1
            continue
        if c in ')]':
            depth -= 1
            i += 1
            continue
        if c == '=' and depth == 0:
            if i + 1 < n and text[i + 1] == '>':
                return ('arrow', i)
            if i + 1 < n and text[i + 1] == '=':
                i += 2
                continue
            return ('assign', i)
        i += 1
    return (None, -1)


def _find_signature_paren(header: str) -> int:
    n = len(header)
    i = 0
    brack = 0
    while i < n:
        c = header[i]
        if c == '"':
            i = skip_string(header, i)
            continue
        if c == "'":
            i = skip_char(header, i)
            continue
        if c == '[':
            brack += 1
        elif c == ']':
            brack -= 1
        elif c == '(' and brack <= 0:
            pre = header[:i].rstrip()
            m = re.search(r'([A-Za-z_]\w*)\s*(<[^<>]*>)?\s*$', pre)
            if m and m.group(1) not in MODIFIER_WORDS:
                return i
            d = 0
            k = i
            while k < n:
                if header[k] == '(':
                    d += 1
                elif header[k] == ')':
                    d -= 1
                    if d == 0:
                        break
                k += 1
            i = k + 1
            continue
        i += 1
    return -1


THIS_RE = re.compile(r'(?<![\w.])this\s*\[')


def classify_member(header: str, member_body: Optional[str], ctype: CSharpType,
                    is_interface: bool):
    header = header.strip()
    if not header:
        return None

    m_this = THIS_RE.search(header)
    if m_this:
        mods, rest = _leading_modifiers(header[:m_this.start()])
        if not _is_public(mods, is_interface):
            return None
        rettype = rest.strip()
        bstart = header.index('[', m_this.start())
        depth = 0
        k = bstart
        while k < len(header):
            if header[k] == '[':
                depth += 1
            elif header[k] == ']':
                depth -= 1
                if depth == 0:
                    break
            k += 1
        params = parse_params(header[bstart + 1:k])
        display = ", ".join(p.render() for p in params)
        return Member(kind='property', name=f"this[{display}]", type=rettype,
                      params=params, modifiers=mods, is_static='static' in mods)

    paren_idx = _find_signature_paren(header)

    if paren_idx != -1:
        pre = header[:paren_idx].rstrip()
        depth = 0
        k = paren_idx
        n = len(header)
        while k < n:
            if header[k] == '(':
                depth += 1
            elif header[k] == ')':
                depth -= 1
                if depth == 0:
                    break
            k += 1
        params_str = header[paren_idx + 1:k]
        after = header[k + 1:].strip()

        mods, rest = _leading_modifiers(pre)
        if not _is_public(mods, is_interface):
            return None

        name_match = re.search(r'([A-Za-z_]\w*)\s*(<[^<>]*>)?\s*$', rest)
        if not name_match:
            return None
        name = name_match.group(1)
        rettype = rest[:name_match.start()].strip()
        generic_suffix = name_match.group(2) or ""

        if name == 'this':
            kind = 'property'
            display_name = f"this[{params_str.strip()}]"
            return Member(kind=kind, name=display_name, type=rettype,
                          modifiers=mods, is_static='static' in mods)

        params = parse_params(params_str)

        if not rettype:
            kind = 'constructor'
            return Member(kind=kind, name=name + generic_suffix, type="",
                          params=params, modifiers=mods,
                          is_static='static' in mods)
        else:
            kind = 'method'
            return Member(kind=kind, name=name + generic_suffix, type=rettype,
                          params=params, modifiers=mods,
                          is_static='static' in mods)

    mods, rest = _leading_modifiers(header)
    if not _is_public(mods, is_interface):
        return None

    is_property = member_body is not None

    if is_property:
        name_match = re.search(r'([A-Za-z_]\w*)\s*$', rest)
        if not name_match:
            return None
        name = name_match.group(1)
        rettype = rest[:name_match.start()].strip()
        return Member(kind='property', name=name, type=rettype,
                      modifiers=mods, is_static='static' in mods)

    op_kind, op_idx = find_first_assign_or_arrow(rest)
    if op_kind == 'arrow':
        name_part = rest[:op_idx].strip()
        name_match = re.search(r'([A-Za-z_]\w*)\s*$', name_part)
        if not name_match:
            return None
        name = name_match.group(1)
        rettype = name_part[:name_match.start()].strip()
        return Member(kind='property', name=name, type=rettype,
                      modifiers=mods, is_static='static' in mods)

    declarators = split_top_level(rest, ',')
    if not declarators:
        return None

    first = declarators[0]
    eq_idx = _find_top_level(first, '=')
    name_part = first[:eq_idx] if eq_idx != -1 else first
    name_match = re.search(r'([A-Za-z_]\w*)\s*(\[\s*\])?\s*$', name_part)
    if not name_match:
        return None
    rettype = name_part[:name_match.start()].strip()
    first_name = name_match.group(1) + (name_match.group(2) or "")

    members = [Member(kind='field', name=first_name, type=rettype,
                      modifiers=mods, is_static='static' in mods)]

    for decl in declarators[1:]:
        eq_idx = _find_top_level(decl, '=')
        nm = decl[:eq_idx] if eq_idx != -1 else decl
        nm = nm.strip()
        m2 = re.match(r'^([A-Za-z_]\w*)\s*(\[\s*\])?$', nm)
        if m2:
            members.append(Member(kind='field',
                                  name=m2.group(1) + (m2.group(2) or ""),
                                  type=rettype, modifiers=mods,
                                  is_static='static' in mods))
    return members


def parse_params(params_str: str) -> List[Param]:
    params_str = params_str.strip()
    if not params_str:
        return []
    parts = split_top_level(params_str, ',')
    result = []
    for part in parts:
        part = part.strip()
        if not part:
            continue
        part = re.sub(r'^\[[^\[\]]*\]\s*', '', part)
        eq_idx = _find_top_level(part, '=')
        default = None
        if eq_idx != -1:
            default = part[eq_idx + 1:].strip()
            part = part[:eq_idx].strip()
        mods, rest = _leading_modifiers(part)
        name_match = re.search(r'([A-Za-z_]\w*)\s*$', rest)
        if not name_match:
            continue
        name = name_match.group(1)
        ptype = rest[:name_match.start()].strip()
        if mods:
            ptype = " ".join(mods) + " " + ptype
        result.append(Param(type=ptype.strip(), name=name, default=default))
    return result


def mask_comments_and_strings(text: str) -> str:
    n = len(text)
    out = list(text)
    i = 0

    def blank(a: int, b: int) -> None:
        for k in range(a, min(b, n)):
            if out[k] != '\n':
                out[k] = ' '

    while i < n:
        c = text[i]
        if c == '"':
            j = skip_string(text, i)
            blank(i, j)
            i = j
            continue
        if c == "'":
            j = skip_char(text, i)
            blank(i, j)
            i = j
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '/':
            j = text.find('\n', i)
            j = j if j != -1 else n
            blank(i, j)
            i = j
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '*':
            j = text.find('*/', i + 2)
            j = (j + 2) if j != -1 else n
            blank(i, j)
            i = j
            continue
        i += 1
    return "".join(out)


@dataclass
class IndexEntry:
    name: str
    kind: str
    bases: "List[str]" = field(default_factory=list)
    doc: XmlDoc = field(default_factory=XmlDoc)
    members: "Dict[str, XmlDoc]" = field(default_factory=dict)


def _bare_type_name(text: str) -> str:
    t = text.strip()
    t = re.sub(r'<.*$', '', t)
    return t.split('.')[-1].strip()


def _member_key(name: str) -> str:
    return re.sub(r'<.*>$', '', name).strip()


def index_all_types(texts: "List[Tuple[str, str]]") -> "Dict[str, IndexEntry]":
    index: Dict[str, IndexEntry] = {}
    for path, text in texts:
        masked = mask_comments_and_strings(text)
        for m in TYPE_HEADER_RE.finditer(masked):
            name = m.group("name")
            brace_open = m.end() - 1
            brace_close = find_matching_brace(text, brace_open)
            if brace_close == -1:
                continue
            body = text[brace_open + 1:brace_close]
            bases_raw = split_bases_and_constraints(m.group("tail") or "")
            bases = [_bare_type_name(b)
                     for b in split_top_level(bases_raw, ',') if b.strip()]
            entry = IndexEntry(
                name=name,
                kind=m.group("kind"),
                bases=bases,
                doc=find_preceding_doc_comment(text, m.start()),
            )
            tmp = CSharpType(kind=m.group("kind"), name=name)
            try:
                if tmp.kind == "enum":
                    for ev in parse_enum_body(body):
                        entry.members.setdefault(ev.name, ev.doc)
                else:
                    parse_type_body(body, tmp)
                    for mem in (tmp.properties + tmp.fields + tmp.methods
                                + tmp.constructors):
                        if not mem.doc.is_empty() or mem.doc.inherit:
                            entry.members.setdefault(_member_key(mem.name), mem.doc)
            except Exception:
                pass
            prev = index.get(name)
            if prev is None or (not prev.members and entry.members):
                index[name] = entry
    return index


def _lookup_inherited(index: "Dict[str, IndexEntry]", start_bases: List[str],
                      member_key: Optional[str],
                      seen: Optional[set] = None) -> Optional[XmlDoc]:
    if seen is None:
        seen = set()
    queue = list(start_bases)
    while queue:
        base = queue.pop(0)
        if base in seen:
            continue
        seen.add(base)
        entry = index.get(base)
        if entry is None:
            continue
        cand = entry.members.get(member_key) if member_key else entry.doc
        if cand is not None and not cand.is_empty():
            if cand.inherit:
                deeper = _lookup_inherited(index, entry.bases, member_key, seen)
                if deeper is not None:
                    merged = XmlDoc(
                        summary=cand.summary, remarks=cand.remarks,
                        returns=cand.returns, value=cand.value,
                        params=dict(cand.params),
                        exceptions=dict(cand.exceptions),
                    )
                    merged.merge_from(deeper)
                    return merged
            return cand
        queue.extend(entry.bases)
    return None


def resolve_inheritdocs(types: "List[CSharpType]",
                        index: "Dict[str, IndexEntry]") -> int:
    resolved = 0
    for ctype in types:
        own_bases = [_bare_type_name(b)
                     for b in split_top_level(ctype.bases, ',') if b.strip()]

        targets: List[Tuple[Optional[str], XmlDoc]] = [(None, ctype.doc)]
        for mem in (ctype.properties + ctype.fields + ctype.methods
                    + ctype.constructors + ctype.enum_values):
            targets.append((_member_key(mem.name), mem.doc))

        for key, doc in targets:
            if not doc.inherit:
                continue
            found = None
            if doc.inherit_cref:
                cref = re.sub(r'^[TMPF]:', '', doc.inherit_cref).split('(')[0]
                bits = [b for b in cref.split('.') if b]
                if key is not None and len(bits) >= 2:
                    found = _lookup_inherited(index, [_bare_type_name(bits[-2])],
                                              bits[-1])
                elif bits:
                    found = _lookup_inherited(index, [_bare_type_name(bits[-1])],
                                              key)
            if found is None:
                found = _lookup_inherited(index, own_bases, key)
            if found is not None:
                doc.merge_from(found)
                doc.inherit = False
                resolved += 1
    return resolved


def md_escape_pipe(text: str) -> str:
    return text.replace('|', '\\|').replace('\n', ' ')


def yaml_escape(text: str) -> str:
    t = " ".join(text.split())
    t = t.replace('\\', '\\\\').replace('"', '\\"')
    if len(t) > 240:
        t = t[:237].rstrip() + "..."
    return t


def code(text: str) -> str:
    return f'`{text}`' if text else ''


def hint(lines: List[str], style: str, text: str) -> None:
    lines.append('{% hint style="' + style + '" %}')
    lines.append(text)
    lines.append('{% endhint %}')
    lines.append("")


def format_signature(m: Member) -> str:
    prefix = "static " if m.is_static else ""
    rettype = f"{m.type} " if m.type else ""
    head = f"{prefix}{rettype}{m.name}"
    if m.kind not in ("method", "constructor"):
        return head
    parts = [p.render() for p in m.params]
    one_line = f"{head}({', '.join(parts)})"
    if len(one_line) <= 72 or not parts:
        return one_line
    body = ",\n".join(f"    {p}" for p in parts)
    return f"{head}(\n{body}\n)"


def overload_labels(members: List[Member]) -> "Dict[int, str]":
    counts: Dict[str, int] = {}
    for m in members:
        counts[m.name] = counts.get(m.name, 0) + 1
    labels: Dict[int, str] = {}
    for idx, m in enumerate(members):
        if counts[m.name] > 1:
            sig = ", ".join(p.type for p in m.params)
            labels[idx] = f"{m.name}({sig})"
        else:
            labels[idx] = m.name
    return labels


def render_type_markdown(ctype: CSharpType) -> str:
    lines: List[str] = []

    if ctype.doc.summary:
        lines.append("---")
        lines.append(f'description: "{yaml_escape(ctype.doc.summary)}"')
        lines.append("---")
        lines.append("")

    lines.append(f"# {ctype.full_name}")
    lines.append("")

    kind_label = {
        "class": "Class",
        "interface": "Interface",
        "struct": "Struct",
        "enum": "Enum",
    }.get(ctype.kind, ctype.kind.capitalize())

    meta = [f"**{kind_label}**"]
    if ctype.namespace:
        meta.append(f"**Namespace** `{ctype.namespace}`")
    if ctype.bases:
        bases = ", ".join(f"`{b.strip()}`"
                          for b in split_top_level(ctype.bases, ',') if b.strip())
        meta.append(f"**Inherits** {bases}")
    lines.append("\\\n".join(meta))
    lines.append("")

    if ctype.doc.summary:
        lines.append(ctype.doc.summary)
        lines.append("")
    if ctype.doc.remarks:
        hint(lines, "info", ctype.doc.remarks)

    if ctype.kind == "enum":
        render_enum_section(lines, ctype)
    else:
        if ctype.constructors:
            render_constructors_section(lines, ctype)
        if ctype.properties:
            render_properties_section(lines, ctype)
        if ctype.fields:
            render_fields_section(lines, ctype)
        if ctype.methods:
            render_methods_section(lines, ctype)

        if not (ctype.constructors or ctype.properties or ctype.fields
                or ctype.methods):
            lines.append("_No public members were found on this type._")
            lines.append("")

    out = "\n".join(lines)
    out = re.sub(r'\n{3,}', '\n\n', out)
    return out.rstrip() + "\n"


def render_enum_section(lines: List[str], ctype: CSharpType) -> None:
    lines.append("## Values")
    lines.append("")
    lines.append("| Name | Value | Description |")
    lines.append("| --- | --- | --- |")
    for m in ctype.enum_values:
        name_cell = f"`{md_escape_pipe(m.name)}`"
        if m.obsolete:
            name_cell = f"~~{name_cell}~~ **(obsolete)**"
        value = f"`{md_escape_pipe(m.enum_value)}`" if m.enum_value else ""
        desc = md_escape_pipe(m.doc.summary)
        lines.append(f"| {name_cell} | {value} | {desc} |")
    lines.append("")


def render_constructors_section(lines: List[str], ctype: CSharpType) -> None:
    lines.append("## Constructors")
    lines.append("")
    labels = overload_labels(ctype.constructors)
    for idx, m in enumerate(ctype.constructors):
        if idx:
            lines.append("---")
            lines.append("")
        label = labels[idx]
        if label != m.name:
            label = label.replace(m.name, ctype.name, 1)
        else:
            label = ctype.name
        render_method_like(lines, m, label)
    lines.append("")


def render_properties_section(lines: List[str], ctype: CSharpType) -> None:
    lines.append("## Properties")
    lines.append("")
    show_access = any(m.accessors for m in ctype.properties)
    lines.append("| Name | Type | " + ("Access | " if show_access else "") + "Description |")
    lines.append("| --- | --- | " + ("--- | " if show_access else "") + "--- |")
    for m in ctype.properties:
        name_cell = f"`{md_escape_pipe(m.name)}`"
        if m.obsolete:
            name_cell = f"~~{name_cell}~~ **(obsolete)**"
        if m.is_static:
            name_cell += " `static`"
        desc = md_escape_pipe(m.doc.summary or m.doc.value)
        row = f"| {name_cell} | `{md_escape_pipe(m.type)}` | "
        if show_access:
            row += (f"`{md_escape_pipe(m.accessors)}` | " if m.accessors else " | ")
        row += f"{desc} |"
        lines.append(row)
    lines.append("")


def render_fields_section(lines: List[str], ctype: CSharpType) -> None:
    lines.append("## Fields")
    lines.append("")
    lines.append("| Name | Type | Description |")
    lines.append("| --- | --- | --- |")
    for m in ctype.fields:
        name_cell = f"`{md_escape_pipe(m.name)}`"
        if m.obsolete:
            name_cell = f"~~{name_cell}~~ **(obsolete)**"
        tags = []
        if 'const' in m.modifiers:
            tags.append('const')
        elif m.is_static:
            tags.append('static')
        if 'readonly' in m.modifiers:
            tags.append('readonly')
        if 'event' in m.modifiers:
            tags.append('event')
        if tags:
            name_cell += " " + " ".join(f"`{t}`" for t in tags)
        desc = md_escape_pipe(m.doc.summary)
        lines.append(f"| {name_cell} | `{md_escape_pipe(m.type)}` | {desc} |")
    lines.append("")


def render_methods_section(lines: List[str], ctype: CSharpType) -> None:
    lines.append("## Methods")
    lines.append("")
    labels = overload_labels(ctype.methods)
    for idx, m in enumerate(ctype.methods):
        if idx:
            lines.append("---")
            lines.append("")
        render_method_like(lines, m, labels[idx])
    lines.append("")


def render_method_like(lines: List[str], m: Member, heading: str) -> None:
    if m.obsolete:
        heading = f"~~{heading}~~"
    lines.append(f"### {heading}")
    lines.append("")

    if m.obsolete:
        hint(lines, "warning",
             "This member is marked `[Obsolete]` and may be removed in a "
             "future version.")

    lines.append("```csharp")
    lines.append(format_signature(m))
    lines.append("```")
    lines.append("")

    if m.doc.summary:
        lines.append(m.doc.summary)
        lines.append("")

    if m.params:
        lines.append("**Parameters**")
        lines.append("")
        lines.append("| Name | Type | Description |")
        lines.append("| --- | --- | --- |")
        for p in m.params:
            desc = md_escape_pipe(m.doc.params.get(p.name, ""))
            name_cell = f"`{p.name}`"
            if p.default:
                name_cell += " _(optional)_"
            lines.append(f"| {name_cell} | `{md_escape_pipe(p.type)}` | {desc} |")
        lines.append("")

    if m.type and m.type != "void":
        lines.append("**Returns**")
        lines.append("")
        ret_desc = m.doc.returns or ""
        lines.append(f"`{m.type}`" + (f" — {ret_desc}" if ret_desc else ""))
        lines.append("")

    if m.doc.exceptions:
        lines.append("**Exceptions**")
        lines.append("")
        lines.append("| Type | Condition |")
        lines.append("| --- | --- |")
        for ex, why in m.doc.exceptions.items():
            lines.append(f"| `{md_escape_pipe(ex)}` | {md_escape_pipe(why)} |")
        lines.append("")

    if m.doc.remarks:
        hint(lines, "info", m.doc.remarks)


def iter_cs_files(root: str):
    if os.path.isfile(root):
        if root.lower().endswith('.cs'):
            yield root
        return
    for dirpath, _dirnames, filenames in os.walk(root):
        for fn in filenames:
            if fn.lower().endswith('.cs'):
                yield os.path.join(dirpath, fn)


def normalize_gitbook_path(gitbook_path: str) -> str:
    p = gitbook_path.strip().strip('/').strip('\\')
    p = p.replace('\\', '/')
    if not p.lower().endswith('.md'):
        p += '.md'
    return p


def output_path_for(gitbook_path: str, output_dir: str) -> str:
    return os.path.normpath(os.path.join(output_dir, normalize_gitbook_path(gitbook_path)))


class SummaryNode:

    def __init__(self) -> None:
        self.entries: "List[Tuple]" = []
        self.folders: "Dict[str, SummaryNode]" = {}
        self.index: "Optional[Tuple[str, str]]" = None

    def get_or_create_folder(self, name: str) -> "SummaryNode":
        child = self.folders.get(name)
        if child is None:
            child = SummaryNode()
            self.folders[name] = child
            self.entries.append(('folder', name, child))
        return child


def split_pascal_case(name: str) -> str:
    s = re.sub(r'(?<=[a-z0-9])(?=[A-Z])', ' ', name)
    s = re.sub(r'(?<=[A-Z])(?=[A-Z][a-z])', ' ', s)
    s = re.sub(r'(?<=[A-Za-z])(?=[0-9])', ' ', s)
    return s


def slug_to_title(slug: str) -> str:
    words = re.split(r'[-_\s]+', slug.strip())
    return " ".join(w[:1].upper() + w[1:] if w else w for w in words if w)


def insert_summary_entry(root: SummaryNode, rel_path: str, title: str) -> None:
    parts = rel_path.split('/')
    node = root
    for seg in parts[:-1]:
        node = node.get_or_create_folder(seg)
    filename = parts[-1]
    stem = filename[:-3] if filename.lower().endswith('.md') else filename
    if stem.lower() == 'readme':
        node.index = (title, rel_path)
    else:
        node.entries.append(('file', title, rel_path))


def build_summary_tree(all_types: List[CSharpType], friendly_titles: bool = False) -> SummaryNode:
    root = SummaryNode()
    for ctype in all_types:
        rel_path = normalize_gitbook_path(ctype.gitbook_path)
        title = split_pascal_case(ctype.name) if friendly_titles else ctype.name
        insert_summary_entry(root, rel_path, title)
    return root


def render_summary_children(node: SummaryNode, indent: int, lines: List[str]) -> None:
    for entry in node.entries:
        if entry[0] == 'file':
            _, title, path = entry
            lines.append(f"{'  ' * indent}* [{title}]({path})")
        else:
            _, name, child = entry
            if child.index:
                title, path = child.index
                lines.append(f"{'  ' * indent}* [{title}]({path})")
            else:
                lines.append(f"{'  ' * indent}* {slug_to_title(name)}")
                print(f"warning: folder '{name}' has no README page; it will "
                      f"appear as plain (unlinked) text in SUMMARY.md",
                      file=sys.stderr)
            render_summary_children(child, indent + 1, lines)


def render_summary(root: SummaryNode, title: str = "Table of contents") -> str:
    lines = [f"# {title}", ""]

    if root.index:
        idx_title, idx_path = root.index
        lines.append(f"* [{idx_title}]({idx_path})")
        lines.append("")

    for entry in root.entries:
        if entry[0] == 'file':
            _, ftitle, fpath = entry
            lines.append(f"* [{ftitle}]({fpath})")
            continue

        _, name, child = entry
        if lines and lines[-1] != "":
            lines.append("")
        heading_title = child.index[0] if child.index else slug_to_title(name)
        lines.append(f"## {heading_title}")
        lines.append("")
        if child.index:
            lines.append(f"* [{heading_title}]({child.index[1]})")
        render_summary_children(child, 0, lines)
        lines.append("")

    out = "\n".join(lines)
    out = re.sub(r'\n{3,}', '\n\n', out)
    return out.rstrip() + "\n"


def main(argv=None):
    parser = argparse.ArgumentParser()
    parser.add_argument("source")
    parser.add_argument("-o", "--output", default="./gitbook_docs")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--no-summary", action="store_true",
                         help="Do not generate a SUMMARY.md file.")
    parser.add_argument("--summary-path", default="SUMMARY.md",
                         help="Path (relative to --output) for the generated "
                              "summary file. Default: SUMMARY.md")
    parser.add_argument("--summary-title", default="Table of contents",
                         help="Heading text at the top of the summary file.")
    parser.add_argument("--friendly-titles", action="store_true",
                         help="In SUMMARY.md, space out PascalCase class names "
                              "(e.g. 'AdminCommands' -> 'Admin Commands'). "
                              "Page headings inside the .md files are unaffected.")
    args = parser.parse_args(argv)

    if not os.path.exists(args.source):
        print(f"error: source path does not exist: {args.source}", file=sys.stderr)
        return 1

    sources: List[Tuple[str, str]] = []
    for cs_file in iter_cs_files(args.source):
        try:
            with open(cs_file, "r", encoding="utf-8-sig") as f:
                sources.append((cs_file, f.read()))
        except (OSError, UnicodeDecodeError) as e:
            print(f"warning: could not read {cs_file}: {e}", file=sys.stderr)

    try:
        doc_index = index_all_types(sources)
    except Exception as e:
        print(f"warning: could not build the inheritdoc index: {e}", file=sys.stderr)
        doc_index = {}

    all_types: List[CSharpType] = []
    for cs_file, text in sources:
        try:
            all_types.extend(find_gitbook_types(text, source_file=cs_file))
        except Exception as e:
            print(f"warning: failed to parse {cs_file}: {e}", file=sys.stderr)

    resolved = resolve_inheritdocs(all_types, doc_index)
    unresolved = sum(
        1 for t in all_types
        for d in [t.doc] + [m.doc for m in (t.properties + t.fields + t.methods
                                            + t.constructors + t.enum_values)]
        if d.inherit
    )
    if resolved or unresolved:
        print(f"inheritdoc: resolved {resolved}, unresolved {unresolved}",
              file=sys.stderr)
    if unresolved:
        print("hint: point the scan at the directory containing the base types "
              "so their docs can be inherited.", file=sys.stderr)

    if not all_types:
        print("No [GitBookPage(...)]-decorated types were found.")
        return 0

    seen_paths: Dict[str, str] = {}
    for ctype in all_types:
        out_path = output_path_for(ctype.gitbook_path, args.output)
        if out_path in seen_paths:
            print(f"warning: duplicate GitBook path '{ctype.gitbook_path}'", file=sys.stderr)
        seen_paths[out_path] = f"{ctype.source_file} ({ctype.name})"

        markdown = render_type_markdown(ctype)

        if args.dry_run:
            print(f"--- {ctype.kind} {ctype.name} -> {out_path} ---")
            print(markdown)
            continue

        os.makedirs(os.path.dirname(out_path), exist_ok=True)
        with open(out_path, "w", encoding="utf-8") as f:
            f.write(markdown)
        print(f"wrote {out_path}  ({ctype.kind} {ctype.name} from {ctype.source_file})")

    if not args.no_summary:
        summary_tree = build_summary_tree(all_types, friendly_titles=args.friendly_titles)
        summary_md = render_summary(summary_tree, title=args.summary_title)
        summary_out_path = os.path.normpath(os.path.join(args.output, args.summary_path))

        if args.dry_run:
            print(f"--- summary -> {summary_out_path} ---")
            print(summary_md)
        else:
            os.makedirs(os.path.dirname(summary_out_path) or ".", exist_ok=True)
            with open(summary_out_path, "w", encoding="utf-8") as f:
                f.write(summary_md)
            print(f"wrote {summary_out_path}  (summary)")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())