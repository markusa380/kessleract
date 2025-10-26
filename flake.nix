{
  description = "Kessleract Scala/SBT project";
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };
  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = import nixpkgs { inherit system; };
      in {
        devShell = pkgs.mkShell {
          buildInputs = [
            pkgs.sbt
            pkgs.openjdk
          ];
          shellHook = ''
            echo "Welcome to the Kessleract development shell!"
          '';
        };
      }
    );
}
