Created: 9/17/19 
Author: Dan Ruley

Design Considerations:
-Dependency graph should be used to represent the relations between cells.
-The only time you need to edit the dependencies is when formulas containing variables are passed in.
-HashSet for non-empty cells

Resource Version Comments:
-PS2 contains minor edits to fix a bug where dependees could not be replaced on an empty graph.

Other Notes:
-Writing tests and sketching things out on pen/paper saved a lot of time on this assignment!

##################################################################################################################################


PS5 Version
Created: 9/25/19
Author: Dan Ruley

Design Considerations:
-Formula input now indicated by string starting with "=...."
-Valid variables now only some >=1 letter followed by >= 1 number, also must be true according to isValid delegate
-Our formulas should be passed Norm/Validator
-Whenever using cell name, use Normalize(name) version
-Refactor tests to meet new specs (eg all formulas begin with "=" and are passed in as strings, not instantiated Formula objects)
-Recalculation should be done all at once, using GetCellsToRecalc list

Resource Version Comments:
-

Other Notes:
-Note to Grader: There are "old" PS4 tests in the test suite, but all of them have been refactored to work with/test the PS5 version of the Spreadsheet class.