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
    const [participants, setParticipants] = useState<Participant[]>([]);
    useEffect(() => {
        setSummoner(null);
        setGames([]);
        setParticipants([]);
        populateSummonerData();
    }, [name]);

    const populateSummonerData = async () => {
        setSummonerLoading(true);
        const response = await fetch(`summoners/${name}`);
        const summoner: any = await response.json();
        setSummoner(summoner);
        setSummonerLoading(false);
    }
    useEffect(() => {
        const getGames = async function () {
            setGamesLoading(true);
            const response = await fetch(`games/${summoner!.puuid}`);
            const games: Game[] = await response.json();
            const participants = games.map(game => game.participants.find(participant => participant.puuid === summoner!.puuid)) as Participant[];
            setParticipants(participants);
            setGames(games);
            setGamesLoading(false);
        }
        if (summoner) {
            getGames();
        }
    }, [summoner]);

    return (
        <div>
            <h1 id="tabelLabel">{name}</h1>
            {/* when the summoner data is loaded, show it */}
            {!summonerLoading && summoner ?
                <div>
                    <p>Summoner Level: {summoner.summonerLevel}</p>
                    <p>Summoner Icon: {summoner.profileIconId}</p>
                </div>
                : <p>Loading...</p>
            }
            {/* when the games data is loaded, show them */}
            {!gamesLoading && games ?
                <div>
                    <h2>Games</h2>
                    <table className='table table-striped' aria-labelledby="tabelLabel">
                        <thead>
                            <tr>
                                <th>Champion</th>
                                <th>Level</th>
                            </tr>
                        </thead>
                        <tbody>
                            {participants.map((p: Participant) =>
                                <tr key={p?.gid}>
                                    <td>{p?.championName}</td>
                                    <td>{p?.champLevel}</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </div>
                : <p>Loading...</p>
            }
        </div>
    );
}