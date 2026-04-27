grammar PLC_expr;

/** The start rule; begin parsing here. */
program: statement+ EOF ;

statement
    : ';'                                               # empty
    | primitiveType IDENTIFIER (',' IDENTIFIER)* ';'   # declaration
    | 'read' IDENTIFIER (',' IDENTIFIER)* ';'          # read
    | 'write' expr (',' expr)* ';'                     # write
    | '{' statement* '}'                               # block
    | 'if' '(' expr ')' statement ('else' statement)?  # ifStmt
    | 'while' '(' expr ')' statement                   # whileStmt
    | 'do' statement 'while' '(' expr ')'              # doWhileStmt
    | 'repeat' statement 'until' '(' expr ')'          # repeatStmt
    | 'for' '(' expr ';' expr ';' expr ')' statement   # forLoop
    | 'abs' '(' expr ')'                               # abs
    | 'toint' '(' expr ')'                             # toint
    | 'break' ';'                                      # break
    | expr ';'                                         # printExpr
    ;

expr
    : IDENTIFIER '++'                      # increment    
    | '-' expr                             # unaryMinus
    | '!' expr                             # not
    | <assoc=right> expr '**' expr         # power
    | expr op=('*'|'/'|'%') expr           # mulDivMod
    | expr op=('+'|'-'|'.') expr           # addSubConcat
    | expr op=('<'|'>') expr               # relational
    | expr op=('=='|'!='|'<>') expr        # equality
    | expr '&&' expr                       # and
    | expr '||' expr                       # or
    | <assoc=right> IDENTIFIER '=' expr    # assignment
    | <assoc=right> IDENTIFIER '+=' expr   # addAssign
    | '(' expr ')'                         # parens
    | IDENTIFIER                           # id
    | INT                                  # int
    | FLOAT                                # float
    | 'true'                               # boolTrue
    | 'false'                              # boolFalse
    | STRING                               # string
    ;

primitiveType
    : 'int'
    | 'float'
    | 'double'
    | 'bool'
    | 'string'
    ;

STRING     : '"' (~["\r\n])* '"' ;
FLOAT      : [0-9]+ '.' [0-9]+ ;
INT        : [0-9]+ ;
IDENTIFIER : [a-zA-Z][a-zA-Z0-9]* ;

LINE_COMMENT : '//' ~[\r\n]* -> skip ;
WS           : [ \t\r\n]+ -> skip ; //toss out whitespace