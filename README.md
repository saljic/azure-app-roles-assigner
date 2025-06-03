# Azure App Roles Assigner

The Azure App Roles Assigner (ARA) is a powerful tool designed to streamline the process of assigning app roles to applications and managed identities. This tool is especially valuable for managed identities, as the Azure portal currently does not support app role assignments for
them.

## Installation

To install the Azure App Roles Assigner, you need to have the .NET SDK installed on your system. If you haven't installed it yet, download it from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).

Once you have the .NET SDK installed, open your terminal or command prompt and run the following command:

```bash
dotnet tool install azure-app-roles-assigner -g
```

The `-g` option installs the tool globally, so you can run it from any location on your system.

## Usage

After installing the tool, you can generate Azure access tokens by running the following command:

```bash
ara
```

This command will invoke the Azure App Roles Assigner in your command line. Follow any prompts or instructions provided by the tool to assign the app roles.

## Contributing

If you'd like to contribute to the Azure Access Token Generator, please fork the repository and submit a pull request. We welcome all improvements, bug fixes, and feature suggestions.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for more information.
