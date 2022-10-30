import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { Summoner as SummonerObj, Game, Participant } from "../../d"
export default function Summoner() {
    const { name } = useParams<{ name: string }>()
    console.log(name);
    const [summoner, setSummoner] = useState<SummonerObj | null>(null);
    const [games, setGames] = useState<Game[]>([]);
    const [summonerLoading, setSummonerLoading] = useState(true);
    const [gamesLoading, setGamesLoading] = useState(true);

    useEffect(() => {
        populateSummonerData();
    }, []);

    const populateSummonerData = async () => {
        setSummonerLoading(true);
        const response = await fetch('summoners');
        const summoner: SummonerObj = await response.json();
        setSummoner(summoner);
        setSummonerLoading(false);
    }
    useEffect(() => {
        const getGames = async function () {
            setGamesLoading(true);
            const response = await fetch('games');
            const games: Game[] = await response.json();
            setGames(games);
            setGamesLoading(false);
        }
        getGames();
    }, [summoner]);

    return (
        <div>
            <h1 id="tabelLabel">{name}</h1>
            {/* when the summoner data is loaded, show it */}
            {!summonerLoading && summoner &&
                <div>
                    <p>Summoner Level: {summoner.summonerLevel}</p>
                    <p>Summoner Icon: {summoner.profileIconId}</p>
                </div>
            }
            {/* when the games data is loaded, show them */}
            {!gamesLoading && games &&
                <div>
                    <h2>Games</h2>
                    <table className='table table-striped' aria-labelledby="tabelLabel">
                        <thead>
                            <tr>
                                <th>Game ID</th>
                                <th>Game Mode</th>
                                <th>Game Type</th>
                                <th>Game Duration</th>
                                <th>Game Creation</th>
                            </tr>
                        </thead>
                        <tbody>
                            {games.map((game: Game) =>
                                <tr key={game.gameID}>
                                    <td>{game.gameID}</td>
                                    <td>{game.gameMode}</td>
                                    <td>{game.gameType}</td>
                                    <td>{game.gameDuration}</td>
                                    <td>{game.gameCreation}</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
            }
        </div>
    );
}