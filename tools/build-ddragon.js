const { writeFileSync } = require('fs');
const { EOL } = require('os');

(async () => {
    // Get latest version
    const versionResponse = await fetch("https://ddragon.leagueoflegends.com/api/versions.json");
    const versions = await versionResponse.json();
    const latestVersion = versions[0];

    console.log(`Fetching champion data for version ${latestVersion}...`);

    // Get champion data
    const championReponse = await fetch(`https://ddragon.leagueoflegends.com/cdn/${latestVersion}/data/en_US/champion.json`);
    const championData = await championReponse.json();

    // Build .cs file
    const fileContent = `namespace AutoAccept.Utils;

public static class DDragon
{
    public static readonly Dictionary<int, string> ChampionNames = new()
    {
${Object.values(championData.data).sort((a, b) => parseInt(a.key, 10) - parseInt(b.key, 10)).map(c => `        { ${c.key}, "${c.name}" }`).join(`,${EOL}`)}
    };
}
`;

    writeFileSync("DDragon.cs", fileContent);
})();
