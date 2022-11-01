import { useState, useEffect } from "react";
import { useParams } from "react-router-dom";
import { Summoner as SummonerObj, Game, Participant } from "../../d"
import Loader from "react-spinners/SyncLoader";
import Info from "./Info";
import "./custom.css";
export default function Summoner() {
    const { name } = useParams<{ name: string }>()
    const [summoner, setSummoner] = useState<SummonerObj | null>(null);
    const [games, setGames] = useState<Game[]>([]);
    const [participants, setParticipants] = useState<Participant[]>([]);
    useEffect(() => {
        setSummoner(null);
        setGames([]);
        setParticipants([]);
        populateSummonerData();
    }, [name]);

    const populateSummonerData = async () => {
        const response = await fetch(`summoners/${name}`);
        const summoner: any = await response.json();
        setSummoner(summoner);
    }
    useEffect(() => {
        const getGames = async function () {
            const response = await fetch(`games/${summoner!.puuid}`);
            const games: Game[] = await response.json();
            const participants = games.map(game => game.participants.find(participant => participant.puuid === summoner!.puuid)) as Participant[];
            setParticipants(participants);
            setGames(games);
        }
        if (summoner) {
            getGames();
        }
    }, [summoner]);

    return (
        <div id="summoner" className="contrast p-3 mb-2 text-black">
            <Info name={name} summoner={summoner} />
            {/* when the games data is loaded, show them */}
            <div className="flex">
                <div className="">placeholder for gen stats</div>
                <div className="w-full">
                    {participants ?
                        <div>
                            <h2>Games</h2>
                            <table className='table table-active table-hover' aria-labelledby="tabelLabel">
                                <thead>
                                    <tr>
                                        <th>Champion</th>
                                        <th>Level</th>
                                        <th>Kills</th>
                                        <th>Deaths</th>
                                        <th>Assists</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {participants.map((p: Participant) =>
                                        <tr className={`win-${p.win}`} key={p?.gid}>
                                            <td>{p?.championName}</td>
                                            <td>{p?.champLevel}</td>
                                            <td>{p?.kills}</td>
                                            <td>{p?.deaths}</td>
                                            <td>{p?.assists}</td>
                                        </tr>
                                    )}
                                </tbody>
                            </table>
                        </div>
                        : <Loader />
                    }
                </div>

            </div>

        </div>
    );
}