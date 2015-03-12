# Introduction #

Using svn trunk isn't for everyone.  Some developers like actual releases.  Here's how to create a DbLinq release.  In the following instructions, `$MAJOR` is the major version number, and `$MINOR` is the minor version number, thus version 0.20 would have `MAJOR=0` and `MINOR=20`.

# Details #

  1. Create a branch:
```
svn cp https://dblinq2007.googlecode.com/svn/trunk https://dblinq2007.googlecode.com/svn/branches/dblinq-$MAJOR-$MINOR
```
  1. Create a tag:
```
svn cp https://dblinq2007.googlecode.com/svn/trunk https://dblinq2007.googlecode.com/svn/tags/dblinq-$MAJOR-$MINOR
```
  1. Grab "pristine" sources:
```
svn co https://dblinq2007.googlecode.com/svn/tags/dblinq-$MAJOR-$MINOR DbLinq-$MAJOR.$MINOR
```
  1. Remove traces of Subversion:
```
find DbLinq-$MAJOR.$MINOR -type d -name .svn -exec rm -Rf {} \;
```
  1. Create `src.zip` file:
```
zip -r DbLinq-$MAJOR.$MINOR-src.zip DbLinq-$MAJOR.$MINOR
```
  1. Build the binaries:
```
MSBuild /p:Configuration=Release DbLinq-%MAJOR%.%MINOR%\src\DbLinq.sln
# or with mono
xbuild  /p:Configuration=Release DbLinq-$MAJOR.$MINOR/src/DbLinq.sln
```
  1. Run the unit tests.  No tests should fail.
```
nunit-x86.exe DbLinq-%MAJOR%-%MINOR%\DbLinq-Sqlite-Sqlserver.nunit
```
  1. Package the binaries:
```
cd DbLinq-$MAJOR.$MINOR
mkdir DbLinq-$MAJOR.$MINOR
cp build/DbLinq* build/DbMetal* DbLinq-$MAJOR.$MINOR
rm DbLinq-$MAJOR.$MINOR/*_test*
zip -r ../DbLinq-$MAJOR.$MINOR.zip DbLinq-$MAJOR.$MINOR
```