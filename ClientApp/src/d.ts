export type Summoner = {
    id: string;
    accountId: string;
    puuid: string;
    name: string;
    profileIconId: number;
    revisionDate: number;
    summonerLevel: number;
}
export type Game = {
    gameCreation: number;
    gameDuration: number;
    gameEndTime: number;
    gameID: number;
    gameMode: string;
    gameName: string;
    gameStartTimestamp: number;
    gameType: string;
    gameVersion: string;
    mapID: number;
    participants: Participant[];
}
export type Participant = {
    assists: number;
    baronKills: number;
    bountyLevel: number;
    champExperience: number;
    champLevel: number;
    championID: number;
    championName: string;
    championTransform: number;
    consumablesPurchased: number;
    damageDealtToBuildings: number;
    damageDealtToObjectives: number;
    damageDealtToTurrets: number;
    damageSelfMitigated: number;
    deaths: number;
    detectorWardsPlaced: number;
    doubleKills: number;
    dragonKills: number;
    firstBloodAssist: boolean;
    firstBloodKill: boolean;
    firstTowerAssist: boolean;
    firstTowerKill: boolean;
    gameEndedInEarlySurrender: boolean;
    gameEndedInSurrender: boolean;
    gid: number;
    goldEarned: number;
    goldSpent: number;
    individualPosition: string;
    inhibitorKills: number;
    puuid: string;
    win: boolean;
}