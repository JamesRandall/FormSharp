# We use linked files in the source tree but the relative paths don't play nicely with how Fable needs things packing
# so we have to make some modifications to proj files to handle the pack

PACKAGE_VERSION=$1

echo "Creating source for packages..."
rm -rf ./packages
mkdir ./packages
mkdir ./packages/FormSharp.Fable.React
cp ./src/FormSharp.Fable.React/*.fs ./packages/FormSharp.Fable.React
cp ./src/FormSharp.Fable.React/*.fsproj ./packages/FormSharp.Fable.React
cp ./src/Common/*.fs ./packages/FormSharp.Fable.React
# Convert linked files to local content
sed -i 's|..\\Common\\||' ./packages/FormSharp.Fable.React/FormSharp.Fable.React.fsproj
sed -i 's|<Link>FormSharp.fs</Link>||' ./packages/FormSharp.Fable.React/FormSharp.Fable.React.fsproj
sed -i 's|<Link>FormSharp.Fable.fs</Link>||' ./packages/FormSharp.Fable.React/FormSharp.Fable.React.fsproj
# Uncomment the Nuget content itemgroup
sed -i 's/<!--<ItemGroup/<ItemGroup/' ./packages/FormSharp.Fable.React/FormSharp.Fable.React.fsproj
sed -i 's/ItemGroup>-->/ItemGroup>/' ./packages/FormSharp.Fable.React/FormSharp.Fable.React.fsproj

echo "Packing..."

dotnet pack ./packages/FormSharp.Fable.React/ -p:PackageVersion=$PACKAGE_VERSION -o ./packages/ --configuration Release
