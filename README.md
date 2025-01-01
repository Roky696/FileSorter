FileSorter
<br />
Console app written in .Net 8.0 

Based on Josef Ottosson post:
<br />
https://josef.codes/sorting-really-large-files-with-c-sharp/
<br />
Rewritten to have better performance.

Tested on 5Gb file with Core I9 9900k 64GB RAM on Samsung 970 EVO Plus 1TB SSD
<br />
Sorting performance observed: ~15Gb/min

usage:

For file creation use -c or --create, specifie file name and size in bytes.
<br />
Example: FileSorter.exe -c test.txt 5000
<br />
Example: FileSorter.exe -create test.txt 5000

For file sorting use -s or --sort, specifie source file name and target file name.
<br />
Example: FileSorter.exe -s test.txt testsorted.txt
<br />
Example: FileSorter.exe -sort test.txt testsorted.txt

For help use:
<br />
Example: FileSorter.exe -h
<br />
Example: FileSorter.exe -help