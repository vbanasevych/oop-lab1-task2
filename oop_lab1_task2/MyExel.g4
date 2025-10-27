grammar MyExel;

/*
 * =============================================================================
 * Parser Rules (Правила Парсера)
 * =============================================================================
 *
 * Початкове правило. Вираз має займати весь вхідний текст (до End-Of-File).
 */
compileUnit : expression EOF;

/*
 * Головне правило для виразів.
 * Пріоритет операцій визначається порядком:
 * 1. Правила, що знаходяться вище, мають вищий пріоритет.
 * 2. Для бінарних операцій (expression op expression) ANTLR
 * автоматично обробляє ліву асоціативність.
 */
expression :
      LPAREN expression RPAREN                                #ParenthesizedExpr
    // 5) inc, dec
    | funcName=(INC | DEC) LPAREN expression RPAREN         #FuncExpr
    // 3) +, - (унарні)
    | op=(ADD | SUBTRACT) expression                        #UnaryExpr
    // 1) *, / та 2) mod, div
    | expression op=(MULTIPLY | DIVIDE | MOD | DIV) expression #MultiplicativeExpr
    // 1) +, - (бінарні)
    | expression op=(ADD | SUBTRACT) expression             #AdditiveExpr
    // Атоми (базові елементи)
    | NUMBER                                                #NumberExpr
    | CELL_REF                                              #CellRefExpr
;


/*
 * =============================================================================
 * Lexer Rules (Правила Лексера)
 * =============================================================================
 */

// 5) inc, dec (зроблено нечутливими до регістру, як в Excel)
INC : [iI][nN][cC];
DEC : [dD][eE][cC];

// 2) mod, div (зроблено нечутливими до регістру)
MOD : [mM][oO][dD];
DIV : [dD][iI][vV];

// 1) +, -, *, /
MULTIPLY : '*';
DIVIDE   : '/';
SUBTRACT : '-';
ADD      : '+';

// Дужки
LPAREN : '(';
RPAREN : ')';

// Посилання на клітинки (напр., A1, B22, AB100)
// [a-zA-Z]+ означає одну або більше літер
// [0-9]+ означає одну або більше цифр
CELL_REF : [a-zA-Z]+[0-9]+;

// 1) цілі числа довільної довжини
// [0-9]+ означає одну або більше цифр
NUMBER : [0-9]+;

// Пропуски (пробіли, таби, нові рядки) ігноруються
WS : [ \t\r\n]+ -> skip;